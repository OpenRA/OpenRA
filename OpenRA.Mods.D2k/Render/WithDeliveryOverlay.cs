#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Render
{
	public class WithDeliveryOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "active";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithDeliveryOverlay(init.self, this); }
	}

	public class WithDeliveryOverlay : INotifyBuildComplete, INotifySold, INotifyDelivery
	{
		WithDeliveryOverlayInfo info;
		Animation overlay;
		bool buildComplete, delivering;

		public WithDeliveryOverlay(Actor self, WithDeliveryOverlayInfo info)
		{
			this.info = info;

			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<IBodyOrientation>();

			buildComplete = !self.HasTrait<Building>(); // always render instantly for units

			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.Play(info.Sequence);
			rs.Add("delivery_overlay_{0}".F(info.Sequence),
				new AnimationWithOffset(overlay,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
					() => !buildComplete),
				info.Palette, info.IsPlayerPalette);
		}

		void PlayDeliveryOverlay()
		{
			if (delivering)
				overlay.PlayThen(info.Sequence, PlayDeliveryOverlay);
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