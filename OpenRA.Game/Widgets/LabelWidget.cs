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
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class LabelWidget : Widget
	{
		public enum TextAlign { Left, Center, Right }
		public enum TextVAlign { Top, Middle, Bottom }
		public string Text = null;
		[Obsolete] public string Background = null;
		public TextAlign Align = TextAlign.Left;
		public TextVAlign VAlign = TextVAlign.Middle;
		public string Font = "Regular";
		public Color Color = Color.White;
		public bool Contrast = false;
		public Color ContrastColor = Color.Black;
		public bool WordWrap = false;
		public Func<string> GetText;
		[Obsolete] public Func<string> GetBackground;
		
		[Obsolete] public bool Bold = false; // Legacy flag. TODO: Remove

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
			
			if (Font == "Regular" && Bold)
				Font = "Bold";
			
			SpriteFont font = Game.Renderer.Fonts[Font];
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