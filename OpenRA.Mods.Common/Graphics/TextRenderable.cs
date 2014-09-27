#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Graphics
{
	public struct TextRenderable : IRenderable
	{
		readonly SpriteFont font;
		readonly WPos pos;
		readonly int zOffset;
		readonly Color color;
		readonly string text;

		public TextRenderable(SpriteFont font, WPos pos, int zOffset, Color color, string text)
		{
			this.font = font;
			this.pos = pos;
			this.zOffset = zOffset;
			this.color = color;
			this.text = text;
		}

		public WPos Pos { get { return pos; } }
		public float Scale { get { return 1f; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithScale(float newScale) { return new TextRenderable(font, pos, zOffset, color, text); }
		public IRenderable WithPalette(PaletteReference newPalette) { return new TextRenderable(font, pos, zOffset, color, text); }
		public IRenderable WithZOffset(int newOffset) { return new TextRenderable(font, pos, zOffset, color, text); }
		public IRenderable OffsetBy(WVec vec) { return new TextRenderable(font, pos + vec, zOffset, color, text); }
		public IRenderable AsDecoration() { return this; }

		public void BeforeRender(WorldRenderer wr) {}
		public void Render(WorldRenderer wr)
		{
			var screenPos = wr.Viewport.Zoom*(wr.ScreenPosition(pos) - wr.Viewport.TopLeft.ToFloat2()) - 0.5f*font.Measure(text).ToFloat2();
			var screenPxPos = new float2((float)Math.Round(screenPos.X), (float)Math.Round(screenPos.Y));
			font.DrawTextWithContrast(text, screenPxPos, color, Color.Black, 1);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var size = font.Measure(text).ToFloat2();
			var offset = wr.ScreenPxPosition(pos) - 0.5f*size;
			Game.Renderer.WorldLineRenderer.DrawRect(offset, offset + size, Color.Red);
		}
	}
}
