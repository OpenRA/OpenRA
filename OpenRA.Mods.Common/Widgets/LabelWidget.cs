#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public enum TextAlign { Left, Center, Right }
	public enum TextVAlign { Top, Middle, Bottom }
	public enum LineVAlign { Top, Middle, Bottom, Collapsed }

	public class LabelWidget : Widget
	{
		[Translate] public string Text = null;
		public TextAlign Align = TextAlign.Left;
		public TextVAlign VAlign = TextVAlign.Middle;
		public string Font = ChromeMetrics.Get<string>("TextFont");
		public Color TextColor = ChromeMetrics.Get<Color>("TextColor");
		public bool Contrast = ChromeMetrics.Get<bool>("TextContrast");
		public Color ContrastColor = ChromeMetrics.Get<Color>("TextContrastColor");
		public bool WordWrap = false;
		public int LineSpacing = 4;
		public LineVAlign LineVAlign = LineVAlign.Middle;
		public Func<string> GetText;
		public Func<Color> GetColor;
		public Func<Color> GetContrastColor;
		public int FontSize { get { return font.Value.Size; } }
		Lazy<SpriteFont> font;

		SpriteFont GetFont()
		{
			SpriteFont font;
			if (!Game.Renderer.Fonts.TryGetValue(Font, out font))
				throw new ArgumentException("Requested font '{0}' was not found.".F(Font));
			return font;
		}

		public LabelWidget()
		{
			GetText = () => Text;
			GetColor = () => TextColor;
			GetContrastColor = () => ContrastColor;
			font = new Lazy<SpriteFont>(GetFont);
		}

		protected LabelWidget(LabelWidget other)
			: base(other)
		{
			Text = other.Text;
			Align = other.Align;
			Font = other.Font;
			TextColor = other.TextColor;
			Contrast = other.Contrast;
			ContrastColor = other.ContrastColor;
			WordWrap = other.WordWrap;
			GetText = other.GetText;
			GetColor = other.GetColor;
			GetContrastColor = other.GetContrastColor;
			font = other.font;
		}

		public int2 MeasureText(string text)
		{
			var textSize = font.Value.Measure(text, LineSpacing);
			if (LineVAlign != LineVAlign.Collapsed)
				textSize += new int2(0, LineSpacing);
			return textSize;
		}

		public override void Draw()
		{
			var text = GetText();
			if (text == null)
				return;

			var textSize = MeasureText(text);
			var position = RenderOrigin;

			if (VAlign == TextVAlign.Middle)
				position += new int2(0, (Bounds.Height - textSize.Y) / 2);

			if (VAlign == TextVAlign.Bottom)
				position += new int2(0, Bounds.Height - textSize.Y);

			if (Align == TextAlign.Center)
				position += new int2((Bounds.Width - textSize.X) / 2, 0);

			if (Align == TextAlign.Right)
				position += new int2(Bounds.Width - textSize.X, 0);

			if (LineVAlign == LineVAlign.Middle)
				position += new int2(0, LineSpacing / 2);

			if (LineVAlign == LineVAlign.Bottom)
				position += new int2(0, LineSpacing);

			if (WordWrap)
				text = WidgetUtils.WrapText(text, Bounds.Width, font.Value);

			var color = GetColor();
			var contrast = GetContrastColor();
			if (Contrast)
				font.Value.DrawTextWithContrast(text, position, color, contrast, 2, LineSpacing);
			else
				font.Value.DrawText(text, position, color, LineSpacing);
		}

		public override Widget Clone() { return new LabelWidget(this); }
	}
}
