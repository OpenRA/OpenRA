#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.Widgets
{
	public enum TextAlign { Left, Center, Right }
	public enum TextVAlign { Top, Middle, Bottom }

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
		public Func<string> GetText;
		public Func<Color> GetColor;
		public Func<Color> GetContrastColor;

		public LabelWidget()
		{
			GetText = () => Text;
			GetColor = () => TextColor;
			GetContrastColor = () => ContrastColor;
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
		}

		public override void Draw()
		{
			SpriteFont font;
			if (!Game.Renderer.Fonts.TryGetValue(Font, out font))
				throw new ArgumentException("Requested font '{0}' was not found.".F(Font));

			var text = GetText();
			if (text == null)
				return;

			var textSize = font.Measure(text);
			var position = RenderOrigin;

			if (VAlign == TextVAlign.Middle)
				position += new int2(0, (Bounds.Height - textSize.Y) / 2);

			if (VAlign == TextVAlign.Bottom)
				position += new int2(0, Bounds.Height - textSize.Y);

			if (Align == TextAlign.Center)
				position += new int2((Bounds.Width - textSize.X) / 2, 0);

			if (Align == TextAlign.Right)
				position += new int2(Bounds.Width - textSize.X, 0);

			if (WordWrap)
				text = WidgetUtils.WrapText(text, Bounds.Width, font);

			var color = GetColor();
			var contrast = GetContrastColor();
			if (Contrast)
				font.DrawTextWithContrast(text, position, color, contrast, 2);
			else
				font.DrawText(text, position, color);
		}

		public override Widget Clone() { return new LabelWidget(this); }
	}
}
