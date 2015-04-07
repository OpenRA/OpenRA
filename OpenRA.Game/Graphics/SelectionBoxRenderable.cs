#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
	public struct SelectionBoxRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly float scale;
		readonly Rectangle visualBounds;
		readonly Color color;

		public SelectionBoxRenderable(Actor actor, Color color)
			: this(actor.CenterPosition, actor.VisualBounds, 1f, color) { }

		public SelectionBoxRenderable(WPos pos, Rectangle visualBounds, float scale, Color color)
		{
			this.pos = pos;
			this.visualBounds = visualBounds;
			this.scale = scale;
			this.color = color;
		}

		public WPos Pos { get { return pos; } }

		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(WVec vec) { return new SelectionBoxRenderable(pos + vec, visualBounds, scale, color); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var screenPos = wr.ScreenPxPosition(pos);
			var tl = screenPos + scale * new float2(visualBounds.Left, visualBounds.Top);
			var br = screenPos + scale * new float2(visualBounds.Right, visualBounds.Bottom);
			var tr = new float2(br.X, tl.Y);
			var bl = new float2(tl.X, br.Y);
			var u = new float2(4f / wr.Viewport.Zoom, 0);
			var v = new float2(0, 4f / wr.Viewport.Zoom);

			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.DrawLine(tl + u, tl, color);
			wlr.DrawLine(tl, tl + v, color);
			wlr.DrawLine(tr, tr - u, color);
			wlr.DrawLine(tr, tr + v, color);

			wlr.DrawLine(bl, bl + u, color);
			wlr.DrawLine(bl, bl - v, color);
			wlr.DrawLine(br, br - u, color);
			wlr.DrawLine(br, br - v, color);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
