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

using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Graphics
{
	public struct RangeCircleRenderable : IRenderable, IFinalizedRenderable
	{
		const int RangeCircleSegments = 32;
		static readonly int[][] RangeCircleStartRotations = Exts.MakeArray(RangeCircleSegments, i => WRot.FromFacing(8 * i).AsMatrix());
		static readonly int[][] RangeCircleEndRotations = Exts.MakeArray(RangeCircleSegments, i => WRot.FromFacing(8 * i + 6).AsMatrix());

		readonly WPos centerPosition;
		readonly WDist radius;
		readonly int zOffset;
		readonly Color color;
		readonly Color contrastColor;

		public RangeCircleRenderable(WPos centerPosition, WDist radius, int zOffset, Color color, Color contrastColor)
		{
			this.centerPosition = centerPosition;
			this.radius = radius;
			this.zOffset = zOffset;
			this.color = color;
			this.contrastColor = contrastColor;
		}

		public WPos Pos { get { return centerPosition; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new RangeCircleRenderable(centerPosition, radius, zOffset, color, contrastColor); }
		public IRenderable WithZOffset(int newOffset) { return new RangeCircleRenderable(centerPosition, radius, newOffset, color, contrastColor); }
		public IRenderable OffsetBy(WVec vec) { return new RangeCircleRenderable(centerPosition + vec, radius, zOffset, color, contrastColor); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			DrawRangeCircle(wr, centerPosition, radius, 1, color, 3, contrastColor);
		}

		public static void DrawRangeCircle(WorldRenderer wr, WPos centerPosition, WDist radius,
			float width, Color color, float contrastWidth, Color contrastColor)
		{
			var wcr = Game.Renderer.WorldRgbaColorRenderer;
			var offset = new WVec(radius.Length, 0, 0);
			for (var i = 0; i < RangeCircleSegments; i++)
			{
				var a = wr.ScreenPosition(centerPosition + offset.Rotate(RangeCircleStartRotations[i]));
				var b = wr.ScreenPosition(centerPosition + offset.Rotate(RangeCircleEndRotations[i]));

				if (contrastWidth > 0)
					wcr.DrawLine(a, b, contrastWidth / wr.Viewport.Zoom, contrastColor);

				if (width > 0)
					wcr.DrawLine(a, b, width / wr.Viewport.Zoom, color);
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
