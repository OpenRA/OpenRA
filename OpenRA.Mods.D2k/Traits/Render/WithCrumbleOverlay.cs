#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	[Desc("Rendered together with the \"make\" animation.")]
	public class WithCrumbleOverlayInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "crumble-overlay";

		[PaletteReference("IsPlayerPalette")]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithCrumbleOverlay(init, this); }
	}

	public class WithCrumbleOverlay : ConditionalTrait<WithCrumbleOverlayInfo>
	{
		public WithCrumbleOverlay(ActorInitializer init, WithCrumbleOverlayInfo info)
			: base(info)
		{
			if (init.Contains<SkipMakeAnimsInit>())
				return;

			var rs = init.Self.Trait<RenderSprites>();

			var overlay = new Animation(init.World, rs.GetImage(init.Self));
			var anim = new AnimationWithOffset(overlay, null, () => IsTraitDisabled);

			// Remove the animation once it is complete
			overlay.PlayThen(info.Sequence, () => init.World.AddFrameEndTask(w => rs.Remove(anim)));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}
	}
}
