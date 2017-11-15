#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Graphics
{
	public struct UITextRenderable : IRenderable, IFinalizedRenderable
	{
		readonly SpriteFont font;
		readonly Color color;
		readonly Color bgDark;
		readonly Color bgLight;
		readonly string text;
		readonly WPos effectiveWorldPos;
		readonly int2 screenPos;
		readonly int zOffset;

		public UITextRenderable(SpriteFont font, WPos effectiveWorldPos, int2 screenPos, int zOffset, Color color, Color bgDark, Color bgLight, string text)
		{
			this.font = font;
			this.effectiveWorldPos = effectiveWorldPos;
			this.screenPos = screenPos;
			this.zOffset = zOffset;
			this.color = color;
			this.bgDark = bgDark;
			this.bgLight = bgLight;
			this.text = text;
		}

		public UITextRenderable(SpriteFont font, WPos effectiveWorldPos, int2 screenPos, int zOffset, Color color, string text)
			: this(font, effectiveWorldPos, screenPos, zOffset, color,
				ChromeMetrics.Get<Color>("TextContrastColorDark"),
				ChromeMetrics.Get<Color>("TextContrastColorLight"),
				text) { }

		// Does not exist in the world, so a world positions don't make sense
		public WPos Pos { get { return effectiveWorldPos; } }
		public WVec Offset { get { return WVec.Zero; } }
		public bool IsDecoration { get { return true; } }

		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new UITextRenderable(font, effectiveWorldPos, screenPos, zOffset, color, bgDark, bgLight, text); }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(WVec vec) { return this; }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			font.DrawTextWithContrast(text, screenPos, color, bgDark, bgLight, 1);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var size = font.Measure(text).ToFloat2();
			Game.Renderer.RgbaColorRenderer.DrawRect(screenPos, screenPos + size, 1, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr) {
			var size = font.Measure(text).ToFloat2();
			return new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)size.X, (int)size.Y);
		}
	}
}
