#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Display a sprite tag when selected.")]
	class FakeInfo : ITraitInfo
	{
		public readonly string Image = "pips";

		[SequenceReference("Image")] public readonly string TagSequence = "tag-fake";

		[PaletteReference] public readonly string Palette = "chrome";

		public object Create(ActorInitializer init) { return new Fake(init.Self, this); }
	}

	class Fake : IPostRenderSelection
	{
		readonly FakeInfo info;
		readonly Actor self;

		public Fake(Actor self, FakeInfo info)
		{
			this.self = self;
			this.info = info;
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			var tagImages = new Animation(wr.World, info.Image);
			var pal = wr.Palette(info.Palette);
			var tagxyOffset = new int2(0, 6);
			tagImages.PlayRepeating(info.TagSequence);
			var b = self.VisualBounds;
			var center = wr.ScreenPxPosition(self.CenterPosition);
			var tm = wr.Viewport.WorldToViewPx(center + new int2((b.Left + b.Right) / 2, b.Top));
			var pos = tm + tagxyOffset - (0.5f * tagImages.Image.Size).ToInt2();
			yield return new UISpriteRenderable(tagImages.Image, pos, 0, pal, 1f);
		}
	}
}
