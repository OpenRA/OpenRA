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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Rendered when a harvester is docked.")]
	public class WithDockedOverlayInfo : PausableConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "docking-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithDockedOverlay(init.Self, this); }
	}

	public class WithDockedOverlay : PausableConditionalTrait<WithDockedOverlayInfo>, INotifyDockHost
	{
		readonly AnimationWithOffset anim;
		bool docked;

		public WithDockedOverlay(Actor self, WithDockedOverlayInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			var overlay = new Animation(self.World, rs.GetImage(self), () => IsTraitPaused);
			overlay.Play(info.Sequence);

			anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self.Orientation))),
				() => IsTraitDisabled || !docked);

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void PlayDockingOverlay()
		{
			if (docked)
				anim.Animation.PlayThen(Info.Sequence, PlayDockingOverlay);
		}

		void INotifyDockHost.Docked(Actor self, Actor client) { docked = true; PlayDockingOverlay(); }
		void INotifyDockHost.Undocked(Actor self, Actor client) { docked = false; }
	}
}
