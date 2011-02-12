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
		
		public string Text = null;
		public string Background = null;
		public TextAlign Align = TextAlign.Left;
		public TextVAlign VAlign = TextVAlign.Middle;
		public bool Bold = false;
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

			if (WordWrap)
			{
				if (textSize.X > Bounds.Width)
				{
					string[] lines = text.Split('\n');
					List<string> newLines = new List<string>();
					int i = 0;
					string line = lines[i++];
					while (true)
					{
						newLines.Add(line);
						int2 m = font.Measure(line);
						int spaceIndex = 0, start = line.Length - 1;
						
						if (m.X <= Bounds.Width)
						{
							if (i < lines.Length - 1)
							{
								line = lines[i++];
								continue;
							}
							else
								break;
						}

						while (m.X > Bounds.Width)
						{
							if (-1 == (spaceIndex = line.LastIndexOf(' ', start)))
								break;
							start = spaceIndex - 1;
							m = font.Measure(line.Substring(0, spaceIndex));
						}

						if (spaceIndex != -1)
						{
							newLines.RemoveAt(newLines.Count - 1);
							newLines.Add(line.Substring(0, spaceIndex));
							line = line.Substring(spaceIndex + 1);
						}
						else if (i < lines.Length - 1)
						{
							line = lines[i++];
							continue;
						}
						else
							break;
					}
					text = string.Join("\n", newLines.ToArray());
				}
			}

			font.DrawText(text, position, Color.White);
		}

		public override Widget Clone() { return new LabelWidget(this); }
	}
}