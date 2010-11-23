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
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class TextFieldWidget : Widget
	{
		public string Text = "";
		public int MaxLength = 0;
		public bool Bold = false;
		public int VisualHeight = 1;
		public Func<bool> OnEnterKey = () => false;
		public Func<bool> OnTabKey = () => false;
		public Action OnLoseFocus = () => { };
		public int CursorPosition { get; protected set; }

		public TextFieldWidget() : base() {}
		protected TextFieldWidget(TextFieldWidget widget)
			: base(widget)
		{
			Text = widget.Text;
			MaxLength = widget.MaxLength;
			Bold = widget.Bold;
			VisualHeight = widget.VisualHeight;
		}

		public override bool LoseFocus(MouseInput mi)
		{
			OnLoseFocus();
			var lose = base.LoseFocus(mi);
			return lose;
		}

		public override bool HandleInputInner(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				return false;

			// Lose focus if they click outside the box; return false so the next widget can grab this event
			if (mi.Event == MouseInputEvent.Down && !RenderBounds.Contains(mi.Location.X, mi.Location.Y) && LoseFocus(mi))
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;

			blinkCycle = 10;
			showCursor = true;
			CursorPosition = ClosestCursorPosition(mi.Location.X);
			return true;
		}
		
		
		public int ClosestCursorPosition(int x)
		{
			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var textSize = font.Measure(Text);
			
			var start = RenderOrigin.X + margin;
			if (textSize.X > Bounds.Width - 2 * margin && Focused)
				start += Bounds.Width - 2 * margin - textSize.X;
			
			int minIndex = -1;
			int minValue = int.MaxValue;
			for (int i = 0; i <= Text.Length; i++)
			{
				var dist = Math.Abs(start + font.Measure(Text.Substring(0,i)).X - x);
				if (dist > minValue)
					break;
				minValue = dist;
				minIndex = i;
			}
			return minIndex;
		}

		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up) return false;
			
			// Only take input if we are focused
			if (!Focused)
				return false;

			if (e.KeyChar == '\r' && OnEnterKey())
				return true;

			if (e.KeyChar == '\t' && OnTabKey())
				return true;

			if (e.KeyName == "left")
			{
				if (CursorPosition > 0)
					CursorPosition--;

				return true;
			}

			if (e.KeyName == "right")
			{
				if (CursorPosition <= Text.Length-1)
					CursorPosition++;

				return true;
			}

			if (e.KeyName == "delete")
			{
				if (Text.Length > 0 && CursorPosition < Text.Length)
				{
					Text = Text.Remove(CursorPosition, 1);
				}
				return true;
			}

			TypeChar(e.KeyChar);
			return true;
		}

		public void TypeChar(char c)
		{
			// backspace
			if (c == '\b' || c == 0x7f)
			{
				if (Text.Length > 0 && CursorPosition > 0)
				{
					Text = Text.Remove(CursorPosition - 1, 1);

					CursorPosition--;
				}
			}
			else if (!char.IsControl(c))
			{
				if (MaxLength > 0 && Text.Length >= MaxLength)
					return;

				Text = Text.Insert(CursorPosition, c.ToString());

				CursorPosition++;
			}
		}

		protected int blinkCycle = 10;
		protected bool showCursor = true;
		public override void Tick()
		{
			if (--blinkCycle <= 0)
			{
				blinkCycle = 20;
				showCursor ^= true;
			}

			base.Tick();
		}
		
		int margin = 5;
		public virtual void DrawWithString(string text)
		{
			if (text == null) text = "";
			
			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var pos = RenderOrigin;

			if (CursorPosition > text.Length)
				CursorPosition = text.Length;

			var textSize = font.Measure(text);
			var cursorPosition = font.Measure(text.Substring(0,CursorPosition));
			
			WidgetUtils.DrawPanel("dialog3",
				new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height));

			// Inset text by the margin and center vertically
			var textPos = pos + new int2(margin, (Bounds.Height - textSize.Y) / 2 - VisualHeight);

			// Right align when editing and scissor when the text overflows
			if (textSize.X > Bounds.Width - 2 * margin)
			{
				if (Focused)
					textPos += new int2(Bounds.Width - 2 * margin - textSize.X, 0);

				Game.Renderer.EnableScissor(pos.X + margin, pos.Y, Bounds.Width - 2 * margin, Bounds.Bottom);
			}

			font.DrawText(text, textPos, Color.White);
			
			if (showCursor && Focused)
				font.DrawText("|", new float2(textPos.X + cursorPosition.X - 2, textPos.Y), Color.White);

			if (textSize.X > Bounds.Width - 2 * margin)
				Game.Renderer.DisableScissor();
		}
		
		public override void DrawInner( WorldRenderer wr )
		{
			DrawWithString(Text);
		}

		public override Widget Clone() { return new TextFieldWidget(this); }
	}
}