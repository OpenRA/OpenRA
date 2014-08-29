#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;

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
		public int MaxLength = 512;
		public int VisualHeight = 1;
		public int LeftMargin = 5;
		public int RightMargin = 5;

		public Func<bool> OnEnterKey = () => false;
		public Func<bool> OnTabKey = () => false;
		public Func<bool> OnEscKey = () => false;
		public Func<bool> OnAltKey = () => false;
		public Action OnLoseFocus = () => { };
		public Action OnTextEdited = () => { };
		public int CursorPosition { get; set; }

		public Func<bool> IsDisabled = () => false;
		public Func<bool> IsValid = () => true;
		public string Font = ChromeMetrics.Get<string>("TextfieldFont");
		public Color TextColor = ChromeMetrics.Get<Color>("TextfieldColor");
		public Color TextColorDisabled = ChromeMetrics.Get<Color>("TextfieldColorDisabled");
		public Color TextColorInvalid = ChromeMetrics.Get<Color>("TextfieldColorInvalid");

		public TextFieldWidget() {}
		protected TextFieldWidget(TextFieldWidget widget)
			: base(widget)
		{
			Text = widget.Text;
			MaxLength = widget.MaxLength;
			Font = widget.Font;
			TextColor = widget.TextColor;
			TextColorDisabled = widget.TextColorDisabled;
			TextColorInvalid = widget.TextColorInvalid;
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

			var minIndex = -1;
			var minValue = int.MaxValue;
			for (var i = 0; i <= apparentText.Length; i++)
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

			if ((e.Key == Keycode.RETURN || e.Key == Keycode.KP_ENTER) && OnEnterKey())
				return true;

			if (e.Key == Keycode.TAB && OnTabKey())
				return true;

			if (e.Key == Keycode.ESCAPE && OnEscKey())
				return true;

			if (e.Key == Keycode.LALT && OnAltKey())
				return true;

			if (e.Key == Keycode.LEFT)
			{
				if (CursorPosition > 0)
					CursorPosition--;

				return true;
			}

			if (e.Key == Keycode.RIGHT)
			{
				if (CursorPosition <= Text.Length-1)
					CursorPosition++;

				return true;
			}

			if (e.Key == Keycode.HOME)
			{
				CursorPosition = 0;
				return true;
			}

			if (e.Key == Keycode.END)
			{
				CursorPosition = Text.Length;
				return true;
			}

			if (e.Key == Keycode.DELETE)
			{
				if (CursorPosition < Text.Length)
				{
					Text = Text.Remove(CursorPosition, 1);
					OnTextEdited();
				}
				return true;
			}

			if (e.Key == Keycode.BACKSPACE && CursorPosition > 0)
			{
				CursorPosition--;
				Text = Text.Remove(CursorPosition, 1);
				OnTextEdited();
				return true;
			}

			if (e.Key == Keycode.V && (e.Modifiers.HasModifier(Modifiers.Ctrl) || e.Modifiers.HasModifier(Modifiers.Meta)))
			{
				using (System.IO.StringReader reader = new System.IO.StringReader(Game.Renderer.Device.GetClipboard()))
					HandleTextInput(reader.ReadLine().Trim());
				return true;
			}

			return true;
		}

		public override bool HandleTextInput(string text)
		{
			if (!HasKeyboardFocus || IsDisabled())
				return false;

			if (MaxLength > 0 && (Text.Length >= MaxLength || text.Length >= MaxLength))
				return true;

			Text = Text.Insert(CursorPosition, text);
			CursorPosition += text.Length;
			OnTextEdited();

			return true;
		}

		protected int blinkCycle = 10;
		protected bool showCursor = true;

		bool wasDisabled;
		public override void Tick()
		{
			// Remove the blicking cursor when disabled
			var isDisabled = IsDisabled();
			if (isDisabled != wasDisabled)
			{
				wasDisabled = isDisabled;
				if (isDisabled && Ui.KeyboardFocusWidget == this)
					YieldKeyboardFocus();
			}

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

				Game.Renderer.EnableScissor(new Rectangle(pos.X + LeftMargin, pos.Y,
					Bounds.Width - LeftMargin - RightMargin, Bounds.Bottom));
			}

			var color = disabled ? TextColorDisabled
			            : IsValid() ? TextColor
			            : TextColorInvalid;
			font.DrawText(apparentText, textPos, color);

			if (showCursor && HasKeyboardFocus)
				font.DrawText("|", new float2(textPos.X + cursorPosition.X - 2, textPos.Y), TextColor);

			if (textSize.X > Bounds.Width - LeftMargin - RightMargin)
				Game.Renderer.DisableScissor();
		}

		public override Widget Clone() { return new TextFieldWidget(this); }
	}
}