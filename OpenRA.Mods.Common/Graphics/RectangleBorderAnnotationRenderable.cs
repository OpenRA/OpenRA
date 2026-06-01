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
	/// <summary>
	/// Draws a complete axis-aligned rectangle border (unlike SelectionBoxAnnotationRenderable corner brackets).
	/// </summary>
	public class RectangleBorderAnnotationRenderable : IRenderable, IFinalizedRenderable
	{
		readonly Rectangle decorationBounds;
		readonly Color color;
		readonly Color contrastColor;
		readonly float width;
		readonly float contrastWidth;

		public RectangleBorderAnnotationRenderable(WPos pos, Rectangle decorationBounds, Color color,
			float width = 1, Color contrastColor = default, float contrastWidth = 0)
		{
			Pos = pos;
			this.decorationBounds = decorationBounds;
			this.color = color;
			this.width = width;
			this.contrastColor = contrastColor;
			this.contrastWidth = contrastWidth;
		}

		public WPos Pos { get; }

		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec) { return new RectangleBorderAnnotationRenderable(Pos + vec, decorationBounds, color, width, contrastColor, contrastWidth); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

		public void Render(WorldRenderer wr)
		{
			var tl = wr.Viewport.WorldToViewPx(new float2(decorationBounds.Left, decorationBounds.Top)).ToFloat2();
			var br = wr.Viewport.WorldToViewPx(new float2(decorationBounds.Right, decorationBounds.Bottom)).ToFloat2();
			var tr = new float2(br.X, tl.Y);
			var bl = new float2(tl.X, br.Y);

			var cr = Game.Renderer.RgbaColorRenderer;
			DrawEdge(cr, tl, tr);
			DrawEdge(cr, tr, br);
			DrawEdge(cr, br, bl);
			DrawEdge(cr, bl, tl);
		}

		void DrawEdge(RgbaColorRenderer cr, in float3 start, in float3 end)
		{
			if (contrastWidth > 0)
				cr.DrawLine(start, end, contrastWidth, contrastColor);

			if (width > 0)
				cr.DrawLine(start, end, width, color);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
