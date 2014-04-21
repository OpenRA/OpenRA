#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class WithHarvestAnimationInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "harvest";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		public object Create(ActorInitializer init) { return new WithHarvestAnimation(init.self, this); }
	}

	class WithHarvestAnimation : INotifyHarvest
	{
		WithHarvestAnimationInfo info;
		Animation anim;
		bool visible;

		public WithHarvestAnimation(Actor self, WithHarvestAnimationInfo info)
		{
			this.info = info;
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<IBodyOrientation>();

			anim = new Animation(rs.GetImage(self), RenderSimple.MakeFacingFunc(self));
			anim.Play(info.Sequence);
			rs.anims.Add("harvest_{0}".F(info.Sequence), new AnimationWithOffset(anim,
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
	}
}
