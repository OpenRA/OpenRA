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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders a decorative overlay animation when resources are spawned.")]
	public class WithResourceSeedingOverlayInfo : UpgradableTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[SequenceReference, Desc("Sequence names to use.")]
		public readonly string[] Sequences = { "active-overlay" };

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Time (in ms) to wait before playing.")]
		public readonly int Delay = 0;

		public override object Create(ActorInitializer init) { return new WithResourceSeedingOverlay(init.Self, this); }
	}

	public class WithResourceSeedingOverlay : UpgradableTrait<WithResourceSeedingOverlayInfo>, INotifyResourceSeeded
	{
		readonly Animation overlay;
		bool played;

		public WithResourceSeedingOverlay(Actor self, WithResourceSeedingOverlayInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			overlay = new Animation(self.World, rs.GetImage(self), () => self.IsDisabled());
			overlay.Play(Info.Sequences.Random(Game.CosmeticRandom));

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => IsTraitDisabled || played,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyResourceSeeded.OnResourceSeeded(Actor self)
		{
			if (IsTraitDisabled)
				return;

			Game.RunAfterDelay(Info.Delay, () => {
				played = false;
				overlay.PlayThen(Info.Sequences.Random(Game.CosmeticRandom), () => { played = true; });
			});
		}
	}
}
