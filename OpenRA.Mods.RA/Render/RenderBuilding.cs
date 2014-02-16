#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
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
		public RenderBuilding(ActorInitializer init, RenderBuildingInfo info)
			: this(init, info, () => 0) { }

		public RenderBuilding(ActorInitializer init, RenderBuildingInfo info, Func<int> baseFacing)
			: base(init.self, baseFacing)
		{
			var self = init.self;

			// Work around a bogus crash
			anim.PlayRepeating(NormalizeSequence(self, "idle"));
			self.Trait<IBodyOrientation>().SetAutodetectedFacings(anim.CurrentSequence.Facings);

			// Can't call Complete() directly from ctor because other traits haven't been inited yet
			if (self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation && !init.Contains<SkipMakeAnimsInit>())
				self.QueueActivity(new MakeAnimation(self, () => Complete(self)));
			else
				self.QueueActivity(new CallFunc(() => Complete(self)));
		}

		void Complete(Actor self)
		{
			anim.PlayRepeating(NormalizeSequence(self, "idle"));
			foreach (var x in self.TraitsImplementing<INotifyBuildComplete>())
				x.BuildingComplete(self);
		}

		public void PlayCustomAnimThen(Actor self, string name, Action a)
		{
			anim.PlayThen(NormalizeSequence(self, name),
				() => { anim.PlayRepeating(NormalizeSequence(self, "idle")); a(); });
		}

		public void PlayCustomAnimRepeating(Actor self, string name)
		{
			anim.PlayThen(NormalizeSequence(self, name),
				() => PlayCustomAnimRepeating(self, name));
		}

		public void PlayCustomAnimBackwards(Actor self, string name, Action a)
		{
			anim.PlayBackwardsThen(NormalizeSequence(self, name),
				() => { anim.PlayRepeating(NormalizeSequence(self, "idle")); a(); });
		}

		public void CancelCustomAnim(Actor self)
		{
			anim.PlayRepeating(NormalizeSequence(self, "idle"));
		}

		public virtual void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (anim.CurrentSequence != null)
				anim.ReplaceAnim(NormalizeSequence(self, "idle"));
		}
	}
}
