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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public class LineAnnotationRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos end;
		readonly float width;
		readonly Color startColor;
		readonly Color endColor;

		public LineAnnotationRenderable(WPos start, WPos end, float width, Color color)
		{
			Pos = start;
			this.end = end;
			this.width = width;
			startColor = endColor = color;
		}

		public LineAnnotationRenderable(WPos start, WPos end, float width, Color startColor, Color endColor)
		{
			Pos = start;
			this.end = end;
			this.width = width;
			this.startColor = startColor;
			this.endColor = endColor;
		}

		public WPos Pos { get; }
		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return new LineAnnotationRenderable(Pos, end, width, startColor, endColor); }
		public IRenderable OffsetBy(in WVec vec) { return new LineAnnotationRenderable(Pos + vec, end + vec, width, startColor, endColor); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			Game.Renderer.RgbaColorRenderer.DrawLine(
				wr.Viewport.WorldToViewPx(wr.ScreenPosition(Pos)),
				wr.Viewport.WorldToViewPx(wr.Screen3DPosition(end)),
				width, startColor, endColor);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
