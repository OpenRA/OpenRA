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
	[Desc("Rendered together with the \"make\" animation.")]
	public class WithCrumbleOverlayInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "crumble-overlay";

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithCrumbleOverlay(init, this); }
	}

	public class WithCrumbleOverlay : ConditionalTrait<WithCrumbleOverlayInfo>
	{
		readonly WithCrumbleOverlayInfo info;
		readonly RenderSprites renderSprites;
		readonly Animation overlay;
		readonly AnimationWithOffset animation;

		public WithCrumbleOverlay(ActorInitializer init, WithCrumbleOverlayInfo info)
			: base(info)
		{
			this.info = info;

			if (init.Contains<SkipMakeAnimsInit>(info))
				return;

			renderSprites = init.Self.Trait<RenderSprites>();

			overlay = new Animation(init.World, renderSprites.GetImage(init.Self));
			animation = new AnimationWithOffset(overlay, null, () => IsTraitDisabled);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (overlay == null)
				return;

			renderSprites.Add(animation, info.Palette, info.IsPlayerPalette);

			// Remove the animation once it is complete
			overlay.PlayThen(info.Sequence, () => self.World.AddFrameEndTask(w => renderSprites.Remove(animation)));
		}
	}
}
