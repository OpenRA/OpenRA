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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Graphics
{
	public struct TextAnnotationRenderable : IRenderable, IFinalizedRenderable
	{
		readonly SpriteFont font;
		readonly WPos pos;
		readonly int zOffset;
		readonly Color color;
		readonly Color bgDark;
		readonly Color bgLight;
		readonly string text;

		public TextAnnotationRenderable(SpriteFont font, WPos pos, int zOffset, Color color, Color bgDark, Color bgLight, string text)
		{
			this.font = font;
			this.pos = pos;
			this.zOffset = zOffset;
			this.color = color;
			this.bgDark = bgDark;
			this.bgLight = bgLight;
			this.text = text;
		}

		public TextAnnotationRenderable(SpriteFont font, WPos pos, int zOffset, Color color, string text)
			: this(font, pos, zOffset, color,
				ChromeMetrics.Get<Color>("TextContrastColorDark"),
				ChromeMetrics.Get<Color>("TextContrastColorLight"),
				text) { }

		public WPos Pos { get { return pos; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithZOffset(int newOffset) { return new TextAnnotationRenderable(font, pos, zOffset, color, text); }
		public IRenderable OffsetBy(WVec vec) { return new TextAnnotationRenderable(font, pos + vec, zOffset, color, text); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var screenPos = wr.Viewport.WorldToViewPx(wr.ScreenPosition(pos)) - 0.5f * font.Measure(text).ToFloat2();
			font.DrawTextWithContrast(text, screenPos, color, bgDark, bgLight, 1);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var size = font.Measure(text).ToFloat2();
			var screenPos = wr.Viewport.WorldToViewPx(wr.ScreenPosition(pos));
			Game.Renderer.RgbaColorRenderer.DrawRect(screenPos - 0.5f * size, screenPos + 0.5f * size, 1, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
