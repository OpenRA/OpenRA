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
using System.Linq;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA.Render
{
	public class RenderBuildingInfo : RenderSimpleInfo, Requires<BuildingInfo>, IPlaceBuildingDecoration
	{
		public readonly bool HasMakeAnimation = true;
		public readonly bool PauseOnLowPower = false;

		public override object Create(ActorInitializer init) { return new RenderBuilding(init, this);}

		public void Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (!ai.Traits.Get<BuildingInfo>().RequiresBaseProvider)
				return;

			foreach (var a in w.ActorsWithTrait<BaseProvider>())
				a.Trait.RenderAfterWorld(wr);
		}
	}

	public class RenderBuilding : RenderSimple, INotifyDamageStateChanged
	{
		RenderBuildingInfo info;

		public RenderBuilding(ActorInitializer init, RenderBuildingInfo info)
			: this(init, info, () => 0) { }

		public RenderBuilding(ActorInitializer init, RenderBuildingInfo info, Func<int> baseFacing)
			: base(init.self, baseFacing)
		{
			var self = init.self;
			this.info = info;

			// Work around a bogus crash
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, "idle"));
			self.Trait<IBodyOrientation>().SetAutodetectedFacings(DefaultAnimation.CurrentSequence.Facings);

			// Can't call Complete() directly from ctor because other traits haven't been inited yet
			if (self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation && !init.Contains<SkipMakeAnimsInit>())
				self.QueueActivity(new MakeAnimation(self, () => Complete(self)));
			else
				self.QueueActivity(new CallFunc(() => Complete(self)));
		}

		void Complete(Actor self)
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, "idle"));
			foreach (var x in self.TraitsImplementing<INotifyBuildComplete>())
				x.BuildingComplete(self);

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
				DefaultAnimation.ReplaceAnim(NormalizeSequence(self, "idle"));
		}
	}
}
