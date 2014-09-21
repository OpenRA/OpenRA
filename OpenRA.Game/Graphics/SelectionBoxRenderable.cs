#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public struct SelectionBoxRenderable : IRenderable
	{
		readonly WPos pos;
		readonly float scale;
		readonly Rectangle bounds;
		readonly Color color;

		public SelectionBoxRenderable(Actor actor, Color color)
			: this(actor.CenterPosition, actor.Bounds.Value, 1f, color) { }

		public SelectionBoxRenderable(WPos pos, Rectangle bounds, float scale, Color color)
		{
			this.pos = pos;
			this.bounds = bounds;
			this.scale = scale;
			this.color = color;
		}

		public WPos Pos { get { return pos; } }

		public float Scale { get { return scale; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithScale(float newScale) { return new SelectionBoxRenderable(pos, bounds, newScale, color); }
		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(WVec vec) { return new SelectionBoxRenderable(pos + vec, bounds, scale, color); }
		public IRenderable AsDecoration() { return this; }

		public void BeforeRender(WorldRenderer wr) {}
		public void Render(WorldRenderer wr)
		{
			var screenPos = wr.ScreenPxPosition(pos);
			var tl = screenPos + scale * new float2(bounds.Left, bounds.Top);
			var br = screenPos + scale * new float2(bounds.Right, bounds.Bottom);
			var tr = new float2(br.X, tl.Y);
			var bl = new float2(tl.X, br.Y);
			var u = new float2(4f / wr.Viewport.Zoom, 0);
			var v = new float2(0, 4f / wr.Viewport.Zoom);

			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.DrawLine(tl + u, tl, color, color);
			wlr.DrawLine(tl, tl + v, color, color);
			wlr.DrawLine(tr, tr - u, color, color);
			wlr.DrawLine(tr, tr + v, color, color);

			wlr.DrawLine(bl, bl + u, color, color);
			wlr.DrawLine(bl, bl - v, color, color);
			wlr.DrawLine(br, br - u, color, color);
			wlr.DrawLine(br, br - v, color, color);
		}

		public void RenderDebugGeometry(WorldRenderer wr) {}
	}
}
