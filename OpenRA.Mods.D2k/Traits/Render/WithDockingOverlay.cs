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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Rendered when a harvester is docked.")]
	public class WithDockingOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "docking-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithDockingOverlay(init.self, this); }
	}

	public class WithDockingOverlay : INotifyDocking, INotifyBuildComplete, INotifySold
	{
		WithDockingOverlayInfo info;
		Animation overlay;
		bool buildComplete, docked;

		public WithDockingOverlay(Actor self, WithDockingOverlayInfo info)
		{
			this.info = info;

			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<IBodyOrientation>();

			buildComplete = !self.HasTrait<Building>(); // always render instantly for units

			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.Play(info.Sequence);
			rs.Add("docking_overlay_{0}".F(info.Sequence),
				new AnimationWithOffset(overlay,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
					() => !buildComplete),
				info.Palette, info.IsPlayerPalette);
		}

		void PlayDockingOverlay()
		{
			if (docked)
				overlay.PlayThen(info.Sequence, PlayDockingOverlay);
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

		public void Docked(Actor self, Actor harvester) { docked = true; PlayDockingOverlay(); }
		public void Undocked(Actor self, Actor harvester) { docked = false; }
	}
}