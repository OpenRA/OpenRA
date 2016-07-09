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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders Ctrl groups using pixel art.")]
	public class WithSpriteControlGroupDecorationInfo : ITraitInfo
	{
		[PaletteReference] public readonly string Palette = "chrome";

		public readonly string Image = "pips";

		[Desc("Sprite sequence used to render the control group 0-9 numbers.")]
		[SequenceReference("Image")] public readonly string GroupSequence = "groups";

		[Desc("Manual offset in screen pixel.")]
		public readonly int2 ScreenOffset = new int2(9, 5);

		public object Create(ActorInitializer init) { return new WithSpriteControlGroupDecoration(init.Self, this); }
	}

	public class WithSpriteControlGroupDecoration : IPostRenderSelection
	{
		public readonly WithSpriteControlGroupDecorationInfo Info;

		readonly Actor self;
		readonly Animation pipImages;

		public WithSpriteControlGroupDecoration(Actor self, WithSpriteControlGroupDecorationInfo info)
		{
			this.self = self;
			Info = info;

			pipImages = new Animation(self.World, Info.Image);
		}

		IEnumerable<IRenderable> IPostRenderSelection.RenderAfterWorld(WorldRenderer wr)
		{
			if (self.World.FogObscures(self))
				yield break;

			if (self.Owner != wr.World.LocalPlayer)
				yield break;

			var b = self.VisualBounds;
			var pos = wr.ScreenPxPosition(self.CenterPosition);
			var tl = wr.Viewport.WorldToViewPx(pos + new int2(b.Left, b.Top));
			var pal = wr.Palette(Info.Palette);

			foreach (var r in DrawControlGroup(wr, self, tl, pal))
				yield return r;
		}

		IEnumerable<IRenderable> DrawControlGroup(WorldRenderer wr, Actor self, int2 basePosition, PaletteReference palette)
		{
			var group = self.World.Selection.GetControlGroupForActor(self);
			if (group == null)
				yield break;

			pipImages.PlayFetchIndex(Info.GroupSequence, () => (int)group);

			var pos = basePosition - (0.5f * pipImages.Image.Size.XY).ToInt2() + Info.ScreenOffset;
			yield return new UISpriteRenderable(pipImages.Image, self.CenterPosition, pos, 0, palette, 1f);
		}
	}
}
