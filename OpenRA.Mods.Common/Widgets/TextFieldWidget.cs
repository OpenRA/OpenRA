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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
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

		public bool Disabled = false;

		public Func<bool> OnEnterKey = () => false;
		public Func<bool> OnTabKey = () => false;
		public Func<bool> OnEscKey = () => false;
		public Func<bool> OnAltKey = () => false;
		public Action OnLoseFocus = () => { };
		public Action OnTextEdited = () => { };
		public int CursorPosition { get; set; }

		public Func<bool> IsDisabled;
		public Func<bool> IsValid = () => true;
		public string Font = ChromeMetrics.Get<string>("TextfieldFont");
		public Color TextColor = ChromeMetrics.Get<Color>("TextfieldColor");
		public Color TextColorDisabled = ChromeMetrics.Get<Color>("TextfieldColorDisabled");
		public Color TextColorInvalid = ChromeMetrics.Get<Color>("TextfieldColorInvalid");

		public TextFieldWidget()
		{
			IsDisabled = () => Disabled;
		}

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
			IsDisabled = widget.IsDisabled;
		}

		public override bool YieldKeyboardFocus()
		{
			OnLoseFocus();
			return base.YieldKeyboardFocus();
		}

		protected void ResetBlinkCycle()
		{
			blinkCycle = 10;
			showCursor = true;
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

			ResetBlinkCycle();
			CursorPosition = ClosestCursorPosition(mi.Location.X);
			return true;
		}

		protected virtual string GetApparentText() { return text; }

		int ClosestCursorPosition(int x)
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

		int GetPrevWhitespaceIndex()
		{
			return Text.Substring(0, CursorPosition).TrimEnd().LastIndexOf(' ') + 1;
		}

		int GetNextWhitespaceIndex()
		{
			var substr = Text.Substring(CursorPosition);
			var substrTrimmed = substr.TrimStart();
			var trimmedSpaces = substr.Length - substrTrimmed.Length;
			var nextWhitespace = substrTrimmed.IndexOf(' ');
			if (nextWhitespace == -1)
				return Text.Length;

			return CursorPosition + trimmedSpaces + nextWhitespace;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (IsDisabled() || e.Event == KeyInputEvent.Up)
				return false;

			// Only take input if we are focused
			if (!HasKeyboardFocus)
				return false;

			var isOSX = Platform.CurrentPlatform == PlatformType.OSX;

			switch (e.Key) {
				case Keycode.RETURN:
				case Keycode.KP_ENTER:
					if (OnEnterKey())
						return true;
					break;

				case Keycode.TAB:
					if (OnTabKey())
						return true;
					break;

				case Keycode.ESCAPE:
					if (OnEscKey())
						return true;
					break;

				case Keycode.LALT:
					if (OnAltKey())
						return true;
					break;

				case Keycode.LEFT:
					ResetBlinkCycle();
					if (CursorPosition > 0)
					{
						if ((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Alt)))
							CursorPosition = GetPrevWhitespaceIndex();
						else if (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))
							CursorPosition = 0;
						else
							CursorPosition--;
					}

					break;

				case Keycode.RIGHT:
					ResetBlinkCycle();
					if (CursorPosition <= Text.Length - 1)
					{
						if ((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Alt)))
							CursorPosition = GetNextWhitespaceIndex();
						else if (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))
							CursorPosition = Text.Length;
						else
							CursorPosition++;
					}

					break;

				case Keycode.HOME:
					ResetBlinkCycle();
					CursorPosition = 0;
					break;

				case Keycode.END:
					ResetBlinkCycle();
					CursorPosition = Text.Length;
					break;

				case Keycode.D:
					if (e.Modifiers.HasModifier(Modifiers.Ctrl) && CursorPosition < Text.Length)
					{
						Text = Text.Remove(CursorPosition, 1);
						OnTextEdited();
					}

					break;

				case Keycode.K:
					// ctrl+k is equivalent to cmd+delete on osx (but also works on osx)
					ResetBlinkCycle();
					if (e.Modifiers.HasModifier(Modifiers.Ctrl) && CursorPosition < Text.Length)
					{
						Text = Text.Remove(CursorPosition);
						OnTextEdited();
					}

					break;

				case Keycode.U:
					// ctrl+u is equivalent to cmd+backspace on osx
					ResetBlinkCycle();
					if (!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl) && CursorPosition > 0)
					{
						Text = Text.Substring(CursorPosition);
						CursorPosition = 0;
						OnTextEdited();
					}

					break;

				case Keycode.X:
					ResetBlinkCycle();
					if (((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))) &&
						!string.IsNullOrEmpty(Text))
					{
						Game.Renderer.SetClipboardText(Text);
						Text = Text.Remove(0);
						CursorPosition = 0;
						OnTextEdited();
					}

					break;

				case Keycode.DELETE:
					// cmd+delete is equivalent to ctrl+k on non-osx
					ResetBlinkCycle();
					if (CursorPosition < Text.Length)
					{
						if ((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Alt)))
							Text = Text.Substring(0, CursorPosition) + Text.Substring(GetNextWhitespaceIndex());
						else if (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))
							Text = Text.Remove(CursorPosition);
						else
							Text = Text.Remove(CursorPosition, 1);

						OnTextEdited();
					}

					break;

				case Keycode.BACKSPACE:
					// cmd+backspace is equivalent to ctrl+u on non-osx
					ResetBlinkCycle();
					if (CursorPosition > 0)
					{
						if ((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Alt)))
						{
							var prevWhitespace = GetPrevWhitespaceIndex();
							Text = Text.Substring(0, prevWhitespace) + Text.Substring(CursorPosition);
							CursorPosition = prevWhitespace;
						}
						else if (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))
						{
							Text = Text.Substring(CursorPosition);
							CursorPosition = 0;
						}
						else
						{
							CursorPosition--;
							Text = Text.Remove(CursorPosition, 1);
						}

						OnTextEdited();
					}

					break;

				case Keycode.V:
					ResetBlinkCycle();
					if ((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Meta)))
					{
						var clipboardText = Game.Renderer.GetClipboardText();

						// Take only the first line of the clipboard contents
						var nl = clipboardText.IndexOf('\n');
						if (nl > 0)
							clipboardText = clipboardText.Substring(0, nl);

						clipboardText = clipboardText.Trim();
						if (clipboardText.Length > 0)
							HandleTextInput(clipboardText);
					}

					break;

				default:
					break;
				}

			return true;
		}

		public override bool HandleTextInput(string text)
		{
			if (!HasKeyboardFocus || IsDisabled())
				return false;

			if (MaxLength > 0 && Text.Length >= MaxLength)
				return true;

			var pasteLength = text.Length;

			// Truncate the pasted string if the total length (current + paste) is greater than the maximum.
			if (MaxLength > 0 && MaxLength > Text.Length)
				pasteLength = Math.Min(text.Length, MaxLength - Text.Length);

			Text = Text.Insert(CursorPosition, text.Substring(0, pasteLength));
			CursorPosition += pasteLength;
			OnTextEdited();

			return true;
		}

		protected int blinkCycle = 10;
		protected bool showCursor = true;

		bool wasDisabled;
		public override void Tick()
		{
			// Remove the blinking cursor when disabled
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

			var color =
				disabled ? TextColorDisabled
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
