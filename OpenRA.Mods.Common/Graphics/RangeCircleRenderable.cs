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

namespace OpenRA.Mods.Common.Graphics
{
	public struct RangeCircleRenderable : IRenderable
	{
		readonly WPos centerPosition;
		readonly WRange radius;
		readonly int zOffset;
		readonly Color color;
		readonly Color contrastColor;

		public RangeCircleRenderable(WPos centerPosition, WRange radius, int zOffset, Color color, Color contrastColor)
		{
			this.centerPosition = centerPosition;
			this.radius = radius;
			this.zOffset = zOffset;
			this.color = color;
			this.contrastColor = contrastColor;
		}

		public WPos Pos { get { return centerPosition; } }
		public float Scale { get { return 1f; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithScale(float newScale) { return new RangeCircleRenderable(centerPosition, radius, zOffset, color, contrastColor); }
		public IRenderable WithPalette(PaletteReference newPalette) { return new RangeCircleRenderable(centerPosition, radius, zOffset, color, contrastColor); }
		public IRenderable WithZOffset(int newOffset) { return new RangeCircleRenderable(centerPosition, radius, newOffset, color, contrastColor); }
		public IRenderable OffsetBy(WVec vec) { return new RangeCircleRenderable(centerPosition + vec, radius, zOffset, color, contrastColor); }
		public IRenderable AsDecoration() { return this; }

		public void BeforeRender(WorldRenderer wr) {}
		public void Render(WorldRenderer wr)
		{
			var wlr = Game.Renderer.WorldLineRenderer;
			var oldWidth = wlr.LineWidth;
			wlr.LineWidth = 3;
			wr.DrawRangeCircle(centerPosition, radius, contrastColor);
			wlr.LineWidth = 1;
			wr.DrawRangeCircle(centerPosition, radius, color);
			wlr.LineWidth = oldWidth;
		}

		public void RenderDebugGeometry(WorldRenderer wr) {}
	}
}
