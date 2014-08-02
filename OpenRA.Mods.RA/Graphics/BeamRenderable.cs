#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA.Graphics
{
	public struct BeamRenderable : IRenderable
	{
		readonly WPos pos;
		readonly int zOffset;
		readonly WVec length;
		readonly Color color;
		readonly float width;

		public BeamRenderable(WPos pos, int zOffset, WVec length, float width, Color color)
		{
			this.pos = pos;
			this.zOffset = zOffset;
			this.length = length;
			this.color = color;
			this.width = width;
		}

		public WPos Pos { get { return pos; } }
		public float Scale { get { return 1f; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithScale(float newScale) { return new BeamRenderable(pos, zOffset, length, width, color); }
		public IRenderable WithPalette(PaletteReference newPalette) { return new BeamRenderable(pos, zOffset, length, width, color); }
		public IRenderable WithZOffset(int newOffset) { return new BeamRenderable(pos, zOffset, length, width, color); }
		public IRenderable OffsetBy(WVec vec) { return new BeamRenderable(pos + vec, zOffset, length, width, color); }
		public IRenderable AsDecoration() { return this; }

		public void BeforeRender(WorldRenderer wr) {}
		public void Render(WorldRenderer wr)
		{
			var wlr = Game.Renderer.WorldLineRenderer;
			var src = wr.ScreenPosition(pos);
			var dest = wr.ScreenPosition(pos + length);

			var oldWidth = wlr.LineWidth;
			wlr.LineWidth = wr.Viewport.Zoom * width;
			wlr.DrawLine(src, dest, color, color);
			wlr.LineWidth = oldWidth;
		}

		public void RenderDebugGeometry(WorldRenderer wr) {}
	}
}
