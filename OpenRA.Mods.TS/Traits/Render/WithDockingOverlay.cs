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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits.Render
{
	[Desc("Rendered on the refinery when a voxel harvester is docking and undocking.")]
	public class WithDockingOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "unload-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithDockingOverlay(init.Self, this); }
	}

	public class WithDockingOverlay
	{
		public readonly WithDockingOverlayInfo Info;
		public readonly AnimationWithOffset WithOffset;

		public bool Visible;

		public WithDockingOverlay(Actor self, WithDockingOverlayInfo info)
		{
			Info = info;

			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			var overlay = new Animation(self.World, rs.GetImage(self));
			overlay.Play(info.Sequence);

			WithOffset = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => !Visible);

			rs.Add(WithOffset, info.Palette, info.IsPlayerPalette);
		}
	}
}