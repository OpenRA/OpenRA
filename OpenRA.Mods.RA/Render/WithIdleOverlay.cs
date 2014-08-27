#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Renders a decorative animation on units and buildings.")]
	public class WithIdleOverlayInfo : ITraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "idle-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public readonly bool PauseOnLowPower = false;

		public object Create(ActorInitializer init) { return new WithIdleOverlay(init.self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var body = init.Actor.Traits.Get<BodyOrientationInfo>();
			var facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 0;
			var anim = new Animation(init.World, image, () => facing);
			anim.PlayRepeating(Sequence);

			var orientation = body.QuantizeOrientation(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing)), facings);
			var offset = body.LocalToWorld(Offset.Rotate(orientation));
			yield return new SpriteActorPreview(anim, offset, offset.Y + offset.Z + 1, p, rs.Scale);
		}
	}

	public class WithIdleOverlay : INotifyDamageStateChanged, INotifyBuildComplete, INotifySold, INotifyTransform
	{
		Animation overlay;
		bool buildComplete;

		public WithIdleOverlay(Actor self, WithIdleOverlayInfo info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<IBodyOrientation>();

			buildComplete = !self.HasTrait<Building>(); // always render instantly for units
			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.PlayRepeating(info.Sequence);
			rs.Add("idle_overlay_{0}".F(info.Sequence),
				new AnimationWithOffset(overlay,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
					() => !buildComplete,
					() => info.PauseOnLowPower && self.IsDisabled(),
					p => WithTurret.ZOffsetFromCenter(self, p, 1)),
				info.Palette, info.IsPlayerPalette);
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		public void Sold(Actor self) { }
		public void Selling(Actor self)
		{
			buildComplete = false;
		}

		public void BeforeTransform(Actor self)
		{
			buildComplete = false;
		}
		public void OnTransform(Actor self) { }
		public void AfterTransform(Actor self) { }

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}
	}
}