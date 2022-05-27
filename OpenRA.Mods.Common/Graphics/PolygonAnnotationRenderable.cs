#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class PolygonAnnotationRenderable : IRenderable, IFinalizedRenderable
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

		public WPos Pos => effectivePos;
		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return new PolygonAnnotationRenderable(vertices, effectivePos, width, color); }

		public IRenderable OffsetBy(in WVec vec)
		{
			// Lambdas can't use 'in' variables, so capture a copy for later
			var offset = vec;
			return new PolygonAnnotationRenderable(vertices.Select(v => v + offset).ToArray(), effectivePos + vec, width, color);
		}

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
