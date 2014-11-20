#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Displays an overlay whenever resources are harvested by the actor.")]
	class WithHarvestAnimationInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "harvest";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		public object Create(ActorInitializer init) { return new WithHarvestAnimation(init.self, this); }
	}

	class WithHarvestAnimation : INotifyHarvesterAction
	{
		WithHarvestAnimationInfo info;
		Animation anim;
		bool visible;

		public WithHarvestAnimation(Actor self, WithHarvestAnimationInfo info)
		{
			this.info = info;
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<IBodyOrientation>();

			anim = new Animation(self.World, rs.GetImage(self), RenderSimple.MakeFacingFunc(self));
			anim.Play(info.Sequence);
			rs.Add("harvest_{0}".F(info.Sequence), new AnimationWithOffset(anim,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => !visible,
				() => false,
				p => WithTurret.ZOffsetFromCenter(self, p, 0)));
		}

		public void Harvested(Actor self, ResourceType resource)
		{
			if (visible)
				return;

			visible = true;
			anim.PlayThen(info.Sequence, () => visible = false);
		}

		public void MovingToResources(Actor self, CPos targetCell, Activity next) { }
		public void MovingToRefinery(Actor self, CPos targetCell, Activity next) { }
		public void MovementCancelled(Actor self) { }
	}
}
