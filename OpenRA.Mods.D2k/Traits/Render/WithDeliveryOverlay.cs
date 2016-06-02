#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits.Render
{
	[Desc("Rendered when ProductionAirdrop is in progress.")]
	public class WithDeliveryOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithDeliveryOverlay(init.Self, this); }
	}

	public class WithDeliveryOverlay : INotifyBuildComplete, INotifySold, INotifyDelivery
	{
		readonly WithDeliveryOverlayInfo info;
		readonly AnimationWithOffset anim;

		bool buildComplete;
		bool delivering;

		public WithDeliveryOverlay(Actor self, WithDeliveryOverlayInfo info)
		{
			this.info = info;

			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			// always render instantly for units
			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>();

			var overlay = new Animation(self.World, rs.GetImage(self));
			overlay.Play(info.Sequence);

			anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => !buildComplete);

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void PlayDeliveryOverlay()
		{
			if (delivering)
				anim.Animation.PlayThen(info.Sequence, PlayDeliveryOverlay);
		}

		public void BuildingComplete(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(120, () =>
				buildComplete = true)));
		}

		public void Sold(Actor self) { }
		public void Selling(Actor self)
		{
			buildComplete = false;
		}

		public void IncomingDelivery(Actor self) { delivering = true; PlayDeliveryOverlay(); }
		public void Delivered(Actor self) { delivering = false; }
	}
}