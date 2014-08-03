#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class RenderBuildingInfo : RenderSimpleInfo, Requires<BuildingInfo>, IPlaceBuildingDecoration
	{
		public readonly bool PauseOnLowPower = false;

		public override object Create(ActorInitializer init) { return new RenderBuilding(init, this);}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (!ai.Traits.Get<BuildingInfo>().RequiresBaseProvider)
				yield break;

			foreach (var a in w.ActorsWithTrait<BaseProvider>())
				foreach (var r in a.Trait.RenderAfterWorld(wr))
					yield return r;
		}
	}

	public class RenderBuilding : RenderSimple, INotifyDamageStateChanged, INotifyBuildComplete
	{
		RenderBuildingInfo info;

		public RenderBuilding(ActorInitializer init, RenderBuildingInfo info)
			: this(init, info, () => 0) { }

		public RenderBuilding(ActorInitializer init, RenderBuildingInfo info, Func<int> baseFacing)
			: base(init.self, baseFacing)
		{
			var self = init.self;
			this.info = info;

			DefaultAnimation.PlayRepeating(NormalizeSequence(self, "idle"));
		}

		public virtual void BuildingComplete(Actor self)
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, "idle"));

			if (info.PauseOnLowPower)
			{
				var disabled = self.TraitsImplementing<IDisable>();
				DefaultAnimation.Paused = () => disabled.Any(d => d.Disabled)
					&& DefaultAnimation.CurrentSequence.Name == NormalizeSequence(self, "idle");
			}
		}

		public void PlayCustomAnimThen(Actor self, string name, Action a)
		{
			DefaultAnimation.PlayThen(NormalizeSequence(self, name),
				() => { DefaultAnimation.PlayRepeating(NormalizeSequence(self, "idle")); a(); });
		}

		public void PlayCustomAnimRepeating(Actor self, string name)
		{
			DefaultAnimation.PlayThen(NormalizeSequence(self, name),
				() => PlayCustomAnimRepeating(self, name));
		}

		public void PlayCustomAnimBackwards(Actor self, string name, Action a)
		{
			DefaultAnimation.PlayBackwardsThen(NormalizeSequence(self, name),
				() => { DefaultAnimation.PlayRepeating(NormalizeSequence(self, "idle")); a(); });
		}

		public void CancelCustomAnim(Actor self)
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, "idle"));
		}

		public virtual void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (DefaultAnimation.CurrentSequence != null)
				DefaultAnimation.ReplaceAnim(NormalizeSequence(self, DefaultAnimation.CurrentSequence.Name));
		}
	}
}
