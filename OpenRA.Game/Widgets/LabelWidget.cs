#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;

namespace OpenRA.Widgets
{
	public class LabelWidget : Widget
	{
		public enum TextAlign { Left, Center, Right }
		public enum TextVAlign { Top, Middle, Bottom }
		
		public string Text = null;
		public string Background = null;
		public TextAlign Align = TextAlign.Left;
		public TextVAlign VAlign = TextVAlign.Middle;
		public bool Bold = false;
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
			GetText = other.GetText;
			GetBackground = other.GetBackground;
		}

		public override void DrawInner()
		{		
			var bg = GetBackground();

			if (bg != null)
				WidgetUtils.DrawPanel(bg, RenderBounds );
						
			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
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

			font.DrawText(text, position, Color.White);
		}

		public override Widget Clone() { return new LabelWidget(this); }
	}
}