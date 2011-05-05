#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class LabelWidget : Widget
	{
		public enum TextAlign { Left, Center, Right }
		public enum TextVAlign { Top, Middle, Bottom }
		public enum LabelFont { Regular, Bold, Title, Tiny, TinyBold, BigBold }
		public string Text = null;
		public string Background = null;
		public TextAlign Align = TextAlign.Left;
		public TextVAlign VAlign = TextVAlign.Middle;
		public LabelFont Font = LabelFont.Regular;
		public Color Color = Color.White;
		public bool Bold = false; // Legacy flag. TODO: Remove
		public bool Contrast = false;
		public Color ContrastColor = Color.Black;
		public bool WordWrap = false;
		public Func<string> GetText;
		public Func<string> GetBackground;
		
		public LabelWidget()
			: base()
		{
			GetText = () => Text;
			GetBackground = () => Background;
		}

		protected LabelWidget(LabelWidget other)
			: base(other)
		{
			Text = other.Text;
			Align = other.Align;
			Bold = other.Bold;
			Font = other.Font;
			Color = other.Color;
			Contrast = other.Contrast;
			ContrastColor = other.ContrastColor;
			WordWrap = other.WordWrap;
			GetText = other.GetText;
			GetBackground = other.GetBackground;
		}

		public override void DrawInner()
		{		
			var bg = GetBackground();

			if (bg != null)
				WidgetUtils.DrawPanel(bg, RenderBounds );
			
			if (Font == LabelFont.Regular && Bold)
				Font = LabelFont.Bold;
			
			// TODO: Hardcoded font types are stupid
			SpriteFont font = Game.Renderer.RegularFont;
			switch (Font)
			{
				case LabelFont.Bold:
					font = Game.Renderer.BoldFont;
				break;
				case LabelFont.Tiny:
					font = Game.Renderer.TinyFont;
				break;
				case LabelFont.TinyBold:
					font = Game.Renderer.TinyBoldFont;
				break;
				case LabelFont.Title:
					font = Game.Renderer.TitleFont;
				break;
				case LabelFont.BigBold:
					font = Game.Renderer.BigBoldFont;
				break;
			}
			var text = GetText();
			if (text == null)
				return;
			
			int2 textSize = font.Measure(text);
			int2 position = RenderOrigin;

			if (VAlign == TextVAlign.Middle)
				position += new int2(0, (Bounds.Height - textSize.Y)/2);
			
			if (VAlign == TextVAlign.Bottom)
				position += new int2(0, Bounds.Height - textSize.Y);
		
			if (Align == TextAlign.Center)
				position += new int2((Bounds.Width - textSize.X)/2, 0);

			if (Align == TextAlign.Right)
				position += new int2(Bounds.Width - textSize.X,0);

			if (WordWrap)
				text = WidgetUtils.WrapText(text, Bounds.Width, font);

			if (Contrast)
				font.DrawTextWithContrast(text, position, Color, ContrastColor, 2);
			else
				font.DrawText(text, position, Color);
		}

		public override Widget Clone() { return new LabelWidget(this); }
	}
}