#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public struct CircleAnnotationRenderable : IRenderable, IFinalizedRenderable
	{
		const int CircleSegments = 32;
		static readonly WVec[] FacingOffsets = Exts.MakeArray(CircleSegments, i => new WVec(1024, 0, 0).Rotate(WRot.FromFacing(i * 256 / CircleSegments)));

		readonly WPos centerPosition;
		readonly WDist radius;
		readonly int width;
		readonly Color color;
		readonly bool filled;

		public CircleAnnotationRenderable(WPos centerPosition, WDist radius, int width, Color color, bool filled = false)
		{
			this.centerPosition = centerPosition;
			this.radius = radius;
			this.width = width;
			this.color = color;
			this.filled = filled;
		}

		public WPos Pos { get { return centerPosition; } }
		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithZOffset(int newOffset) { return new CircleAnnotationRenderable(centerPosition, radius, width, color, filled); }
		public IRenderable OffsetBy(WVec vec) { return new CircleAnnotationRenderable(centerPosition + vec, radius, width, color, filled); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var cr = Game.Renderer.RgbaColorRenderer;
			if (filled)
			{
				var offset = new WVec(radius.Length, radius.Length, 0);
				var tl = wr.Viewport.WorldToViewPx(wr.ScreenPosition(centerPosition - offset));
				var br = wr.Viewport.WorldToViewPx(wr.ScreenPosition(centerPosition + offset));

				cr.FillEllipse(tl, br, color);
			}
			else
			{
				var r = radius.Length;
				var a = wr.Viewport.WorldToViewPx(wr.ScreenPosition(centerPosition + r * FacingOffsets[CircleSegments - 1] / 1024));
				for (var i = 0; i < CircleSegments; i++)
				{
					var b = wr.Viewport.WorldToViewPx(wr.ScreenPosition(centerPosition + r * FacingOffsets[i] / 1024));
					cr.DrawLine(a, b, width, color);
					a = b;
				}
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
