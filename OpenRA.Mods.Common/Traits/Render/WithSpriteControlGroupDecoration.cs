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

		[Desc("Point in the actor's selection box used as reference for offsetting the decoration image. " +
			"Possible values are combinations of Center, Top, Bottom, Left, Right.")]
		public readonly ReferencePoints ReferencePoint = ReferencePoints.Top | ReferencePoints.Left;

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

			var pal = wr.Palette(Info.Palette);
			foreach (var r in DrawControlGroup(wr, self, pal))
				yield return r;
		}

		IEnumerable<IRenderable> DrawControlGroup(WorldRenderer wr, Actor self, PaletteReference palette)
		{
			var group = self.World.Selection.GetControlGroupForActor(self);
			if (group == null)
				yield break;

			pipImages.PlayFetchIndex(Info.GroupSequence, () => (int)group);

			var bounds = self.VisualBounds;
			var halfSize = (0.5f * pipImages.Image.Size.XY).ToInt2();

			var boundsOffset = new int2(bounds.Left + bounds.Right, bounds.Top + bounds.Bottom) / 2;
			var sizeOffset = -halfSize;
			if (Info.ReferencePoint.HasFlag(ReferencePoints.Top))
			{
				boundsOffset -= new int2(0, bounds.Height / 2);
				sizeOffset += new int2(0, halfSize.Y);
			}
			else if (Info.ReferencePoint.HasFlag(ReferencePoints.Bottom))
			{
				boundsOffset += new int2(0, bounds.Height / 2);
				sizeOffset -= new int2(0, halfSize.Y);
			}

			if (Info.ReferencePoint.HasFlag(ReferencePoints.Left))
			{
				boundsOffset -= new int2(bounds.Width / 2, 0);
				sizeOffset += new int2(halfSize.X, 0);
			}
			else if (Info.ReferencePoint.HasFlag(ReferencePoints.Right))
			{
				boundsOffset += new int2(bounds.Width / 2, 0);
				sizeOffset -= new int2(halfSize.X, 0);
			}

			var pxPos = wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(self.CenterPosition) + boundsOffset) + sizeOffset;
			yield return new UISpriteRenderable(pipImages.Image, self.CenterPosition, pxPos, 0, palette, 1f);
		}
	}
}
