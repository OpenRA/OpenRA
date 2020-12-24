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

using System;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public enum TextAlign { Left, Center, Right }
	public enum TextVAlign { Top, Middle, Bottom }

	public class LabelWidget : Widget
	{
		public string Text = null;
		public TextAlign Align = TextAlign.Left;
		public TextVAlign VAlign = TextVAlign.Middle;
		public string Font = ChromeMetrics.Get<string>("TextFont");
		public Color TextColor = ChromeMetrics.Get<Color>("TextColor");
		public bool Contrast = ChromeMetrics.Get<bool>("TextContrast");
		public bool Shadow = ChromeMetrics.Get<bool>("TextShadow");
		public Color ContrastColorDark = ChromeMetrics.Get<Color>("TextContrastColorDark");
		public Color ContrastColorLight = ChromeMetrics.Get<Color>("TextContrastColorLight");
		public int ContrastRadius = ChromeMetrics.Get<int>("TextContrastRadius");
		public bool WordWrap = false;
		public Func<string> GetText;
		public Func<Color> GetColor;
		public Func<Color> GetContrastColorDark;
		public Func<Color> GetContrastColorLight;

		public LabelWidget()
		{
			GetText = () => Text;
			GetColor = () => TextColor;
			GetContrastColorDark = () => ContrastColorDark;
			GetContrastColorLight = () => ContrastColorLight;
		}

		protected LabelWidget(LabelWidget other)
			: base(other)
		{
			Text = other.Text;
			Align = other.Align;
			VAlign = other.VAlign;
			Font = other.Font;
			TextColor = other.TextColor;
			Contrast = other.Contrast;
			ContrastColorDark = other.ContrastColorDark;
			ContrastColorLight = other.ContrastColorLight;
			ContrastRadius = other.ContrastRadius;
			Shadow = other.Shadow;
			WordWrap = other.WordWrap;
			GetText = other.GetText;
			GetColor = other.GetColor;
			GetContrastColorDark = other.GetContrastColorDark;
			GetContrastColorLight = other.GetContrastColorLight;
		}

		public override void Draw()
		{
			if (!Game.Renderer.Fonts.TryGetValue(Font, out var font))
				throw new ArgumentException("Requested font '{0}' was not found.".F(Font));

			var text = GetText();
			if (text == null)
				return;

			var textSize = font.Measure(text);
			var position = RenderOrigin;
			var offset = font.TopOffset;

			if (VAlign == TextVAlign.Top)
				position += new int2(0, -offset);

			if (VAlign == TextVAlign.Middle)
				position += new int2(0, (Bounds.Height - textSize.Y - offset) / 2);

			if (VAlign == TextVAlign.Bottom)
				position += new int2(0, Bounds.Height - textSize.Y);

			if (Align == TextAlign.Center)
				position += new int2((Bounds.Width - textSize.X) / 2, 0);

			if (Align == TextAlign.Right)
				position += new int2(Bounds.Width - textSize.X, 0);

			if (WordWrap)
				text = WidgetUtils.WrapText(text, Bounds.Width, font);

			DrawInner(text, font, GetColor(), position);
		}

		protected virtual void DrawInner(string text, SpriteFont font, Color color, int2 position)
		{
			var bgDark = GetContrastColorDark();
			var bgLight = GetContrastColorLight();
			if (Contrast)
				font.DrawTextWithContrast(text, position, color, bgDark, bgLight, ContrastRadius);
			else if (Shadow)
				font.DrawTextWithShadow(text, position, color, bgDark, bgLight, 1);
			else
				font.DrawText(text, position, color);
		}

		public override Widget Clone() { return new LabelWidget(this); }
	}
}
