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

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public struct PolygonAnnotationRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos[] vertices;
		readonly WPos effectivePos;
		readonly int width;
		readonly Color color;

		public PolygonAnnotationRenderable(WPos[] vertices, WPos effectivePos, int width, Color color)
		{
			this.vertices = vertices;
			this.effectivePos = effectivePos;
			this.width = width;
			this.color = color;
		}

		public WPos Pos { get { return effectivePos; } }
		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithZOffset(int newOffset) { return new PolygonAnnotationRenderable(vertices, effectivePos, width, color); }
		public IRenderable OffsetBy(WVec vec) { return new PolygonAnnotationRenderable(vertices.Select(v => v + vec).ToArray(), effectivePos + vec, width, color); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var verts = vertices.Select(v => wr.Viewport.WorldToViewPx(wr.ScreenPosition(v)).ToFloat2()).ToArray();
			Game.Renderer.RgbaColorRenderer.DrawPolygon(verts, width, color);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
