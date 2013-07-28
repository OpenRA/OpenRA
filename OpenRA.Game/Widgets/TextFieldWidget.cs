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
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class TextFieldWidget : Widget
	{
		string text = "";
		public string Text
		{
			get { return text; }
			set { text = value ?? ""; CursorPosition = CursorPosition.Clamp(0, text.Length); }
		}

		public int MaxLength = 0;
		public int VisualHeight = 1;
		public int LeftMargin = 5;
		public int RightMargin = 5;

		public Func<bool> OnEnterKey = () => false;
		public Func<bool> OnTabKey = () => false;
		public Func<bool> OnEscKey = () => false;
		public Action OnLoseFocus = () => { };
		public int CursorPosition { get; protected set; }

		public Func<bool> IsDisabled = () => false;
		public Color TextColor = Color.White;
		public Color DisabledColor = Color.Gray;
		public string Font = "Regular";

		public TextFieldWidget() : base() {}
		protected TextFieldWidget(TextFieldWidget widget)
			: base(widget)
		{
			Text = widget.Text;
			MaxLength = widget.MaxLength;
			Font = widget.Font;
			TextColor = widget.TextColor;
			DisabledColor = widget.DisabledColor;
			VisualHeight = widget.VisualHeight;
		}

		public override bool YieldKeyboardFocus()
		{
			OnLoseFocus();
			return base.YieldKeyboardFocus();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (IsDisabled())
				return false;

			if (mi.Event != MouseInputEvent.Down)
				return false;

			// Attempt to take keyboard focus
			if (!RenderBounds.Contains(mi.Location) || !TakeKeyboardFocus())
				return false;

			blinkCycle = 10;
			showCursor = true;
			CursorPosition = ClosestCursorPosition(mi.Location.X);
			return true;
		}

		protected virtual string GetApparentText() { return text; }

		public int ClosestCursorPosition(int x)
		{
			var apparentText = GetApparentText();
			var font = Game.Renderer.Fonts[Font];
			var textSize = font.Measure(apparentText);

			var start = RenderOrigin.X + LeftMargin;
			if (textSize.X > Bounds.Width - LeftMargin - RightMargin && HasKeyboardFocus)
				start += Bounds.Width - LeftMargin - RightMargin - textSize.X;

			int minIndex = -1;
			int minValue = int.MaxValue;
			for (int i = 0; i <= apparentText.Length; i++)
			{
				var dist = Math.Abs(start + font.Measure(apparentText.Substring(0, i)).X - x);
				if (dist > minValue)
					break;
				minValue = dist;
				minIndex = i;
			}
			return minIndex;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (IsDisabled() || e.Event == KeyInputEvent.Up)
				return false;

			// Only take input if we are focused
			if (!HasKeyboardFocus)
				return false;

			if ((e.KeyName == "return" || e.KeyName == "enter") && OnEnterKey())
				return true;

			if (e.KeyName == "tab" && OnTabKey())
				return true;

			if (e.KeyName == "escape" && OnEscKey())
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
				if (CursorPosition < Text.Length)
					Text = Text.Remove(CursorPosition, 1);
				return true;
			}

			TypeChar(e);
			return true;
		}

		public void TypeChar(KeyInput key)
		{
			if (key.KeyName == "backspace" && CursorPosition > 0)
			{
				CursorPosition--;
				Text = Text.Remove(CursorPosition, 1);
			}

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
		}

		public override void Draw()
		{
			var apparentText = GetApparentText();
			var font = Game.Renderer.Fonts[Font];
			var pos = RenderOrigin;

			var textSize = font.Measure(apparentText);
			var cursorPosition = font.Measure(apparentText.Substring(0, CursorPosition));

			var disabled = IsDisabled();
			var state = disabled ? "textfield-disabled" :
				HasKeyboardFocus ? "textfield-focused" :
				Ui.MouseOverWidget == this ? "textfield-hover" :
				"textfield";

			WidgetUtils.DrawPanel(state,
				new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height));

			// Inset text by the margin and center vertically
			var textPos = pos + new int2(LeftMargin, (Bounds.Height - textSize.Y) / 2 - VisualHeight);

			// Right align when editing and scissor when the text overflows
			if (textSize.X > Bounds.Width - LeftMargin - RightMargin)
			{
				if (HasKeyboardFocus)
					textPos += new int2(Bounds.Width - LeftMargin - RightMargin - textSize.X, 0);

				Game.Renderer.EnableScissor(pos.X + LeftMargin, pos.Y,
					Bounds.Width - LeftMargin - RightMargin, Bounds.Bottom);
			}

			var color = disabled ? DisabledColor : TextColor;
			font.DrawText(apparentText, textPos, color);

			if (showCursor && HasKeyboardFocus)
				font.DrawText("|", new float2(textPos.X + cursorPosition.X - 2, textPos.Y), Color.White);

			if (textSize.X > Bounds.Width - LeftMargin - RightMargin)
				Game.Renderer.DisableScissor();
		}

		public override Widget Clone() { return new TextFieldWidget(this); }
	}
}