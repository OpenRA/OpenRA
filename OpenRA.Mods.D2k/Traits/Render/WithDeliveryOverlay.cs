#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits.Render
{
	[Desc("Rendered when ProductionAirdrop is in progress.")]
	public class WithDeliveryOverlayInfo : PausableConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "active";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithDeliveryOverlay(init.Self, this); }
	}

	public class WithDeliveryOverlay : PausableConditionalTrait<WithDeliveryOverlayInfo>, INotifyDelivery
	{
		readonly AnimationWithOffset anim;
		bool delivering;

		public WithDeliveryOverlay(Actor self, WithDeliveryOverlayInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			var overlay = new Animation(self.World, rs.GetImage(self), () => IsTraitPaused);
			overlay.Play(info.Sequence);

			// These translucent overlays should not be included in highlight flashes
			overlay.IsDecoration = true;

			anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self.Orientation))),
				() => IsTraitDisabled || !delivering);

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void PlayDeliveryOverlay()
		{
			if (delivering)
				anim.Animation.PlayThen(Info.Sequence, PlayDeliveryOverlay);
		}

		void INotifyDelivery.IncomingDelivery(Actor self) { delivering = true; PlayDeliveryOverlay(); }
		void INotifyDelivery.Delivered(Actor self) { delivering = false; }
	}
}
