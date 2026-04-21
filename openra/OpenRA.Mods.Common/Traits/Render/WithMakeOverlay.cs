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
	[Desc("Draws an overlay on top of a make animation.")]
	public class WithMakeOverlayInfo : TraitInfo
	{
		[SequenceReference]
		[FieldLoader.Require]
		[Desc("Sequence name to use.")]
		public readonly string Sequence = null;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name.")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithMakeOverlay(init.Self, this); }
	}

	public class WithMakeOverlay
	{
		readonly WithMakeOverlayInfo info;
		readonly AnimationWithOffset anim;
		bool visible;

		public WithMakeOverlay(Actor self, WithMakeOverlayInfo info)
		{
			this.info = info;

			var rs = self.Trait<RenderSprites>();
			var overlay = new Animation(self.World, rs.GetImage(self));
			overlay.Play(info.Sequence);

			anim = new AnimationWithOffset(overlay, null, () => !visible);
			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		public void Forward()
		{
			visible = true;
			anim.Animation.PlayThen(info.Sequence, () => visible = false);
		}

		public void Reverse()
		{
			visible = true;
			anim.Animation.PlayBackwardsThen(info.Sequence, () => visible = false);
		}
	}
}
