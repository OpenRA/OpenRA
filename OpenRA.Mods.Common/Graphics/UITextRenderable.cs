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
	public struct UITextRenderable : IRenderable, IFinalizedRenderable
	{
		readonly SpriteFont font;
		readonly WPos effectiveWorldPos;
		readonly int2 screenPos;
		readonly int zOffset;
		readonly Color color;
		readonly Color bgDark;
		readonly Color bgLight;
		readonly string text;

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

		public WPos Pos { get { return effectiveWorldPos; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithZOffset(int newOffset) { return new UITextRenderable(font, effectiveWorldPos, screenPos, zOffset, color, text); }
		public IRenderable OffsetBy(WVec vec) { return new UITextRenderable(font, effectiveWorldPos + vec, screenPos, zOffset, color, text); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			font.DrawTextWithContrast(text, screenPos, color, bgDark, bgLight, 1);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var size = font.Measure(text).ToFloat2();
			Game.Renderer.RgbaColorRenderer.DrawRect(screenPos - 0.5f * size, screenPos + 0.5f * size, 1, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
