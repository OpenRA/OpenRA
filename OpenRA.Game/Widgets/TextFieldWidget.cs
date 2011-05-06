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
	public class TextFieldWidget : Widget
	{
		public string Text = "";
		public int MaxLength = 0;
		public bool Bold = false;
		public int VisualHeight = 1;
		public string Background = "dialog3";
		public int LeftMargin = 5;
		public int RightMargin = 5;
		
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

		// TODO: TextFieldWidgets don't support delegate methods for mouse input
		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				return false;

			// Lose focus if they click outside the box; return false so the next widget can grab this event
			if (mi.Event == MouseInputEvent.Down && !RenderBounds.Contains(mi.Location) && LoseFocus(mi))
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
			if (Text == null)
				return 0;
			
			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var textSize = font.Measure(Text);
			
			var start = RenderOrigin.X + LeftMargin;
			if (textSize.X > Bounds.Width - LeftMargin - RightMargin && Focused)
				start += Bounds.Width - LeftMargin - RightMargin - textSize.X;
			
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

			if ((e.KeyName == "return" || e.KeyName == "enter") && OnEnterKey())
				return true;

			if (e.KeyName == "tab" && OnTabKey())
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
			
			if (e.KeyName == "home")
			{
				CursorPosition = 0;
				return true;
			}
			
			if (e.KeyName == "end")
			{
				CursorPosition = Text.Length;
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

			TypeChar(e);
			return true;
		}

		public void TypeChar(KeyInput key)
		{
			if (Text == null)
				Text = "";
			
			if (key.KeyName == "backspace" && Text.Length > 0 && CursorPosition > 0)
			{
				Text = Text.Remove(CursorPosition - 1, 1);
				CursorPosition--;
			}
			else if (key.KeyName == "delete" && Text.Length > 0 && CursorPosition < Text.Length - 1)
				Text = Text.Remove(CursorPosition, 1);
		
			else if (key.IsValidInput())
			{	
				if (MaxLength > 0 && Text.Length >= MaxLength)
					return;

				Text = Text.Insert(CursorPosition, key.UnicodeChar.ToString());

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
		
		public virtual void DrawWithString(string text)
		{
			if (text == null) text = "";
			
			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var pos = RenderOrigin;

			if (CursorPosition > text.Length)
				CursorPosition = text.Length;

			var textSize = font.Measure(text);
			var cursorPosition = font.Measure(text.Substring(0,CursorPosition));
			
			WidgetUtils.DrawPanel(Background,
				new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height));

			// Inset text by the margin and center vertically
			var textPos = pos + new int2(LeftMargin, (Bounds.Height - textSize.Y) / 2 - VisualHeight);

			// Right align when editing and scissor when the text overflows
			if (textSize.X > Bounds.Width - LeftMargin - RightMargin)
			{
				if (Focused)
					textPos += new int2(Bounds.Width - LeftMargin - RightMargin - textSize.X, 0);

				Game.Renderer.EnableScissor(pos.X + LeftMargin, pos.Y, Bounds.Width - LeftMargin - RightMargin, Bounds.Bottom);
			}

			font.DrawText(text, textPos, Color.White);
			
			if (showCursor && Focused)
				font.DrawText("|", new float2(textPos.X + cursorPosition.X - 2, textPos.Y), Color.White);

			if (textSize.X > Bounds.Width - LeftMargin - RightMargin)
				Game.Renderer.DisableScissor();
		}
		
		public override void DrawInner()
		{
			DrawWithString(Text);
		}

		public override Widget Clone() { return new TextFieldWidget(this); }
	}
}