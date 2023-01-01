#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public enum TextFieldType { General, Filename, Integer }
	public class TextFieldWidget : InputWidget
	{
		string text = "";
		public string Text
		{
			get => text;

			set
			{
				text = RemoveInvalidCharacters(value ?? "");
				CursorPosition = CursorPosition.Clamp(0, text.Length);
				ClearSelection();
			}
		}

		public int MaxLength = 0;
		public int VisualHeight = 1;
		public int LeftMargin = 5;
		public int RightMargin = 5;
		public string Background = "textfield";

		TextFieldType type = TextFieldType.General;
		public TextFieldType Type
		{
			get => type;

			set
			{
				type = value;

				// Revalidate text
				text = RemoveInvalidCharacters(text);
				CursorPosition = CursorPosition.Clamp(0, text.Length);
			}
		}

		public Func<KeyInput, bool> OnEnterKey = _ => false;
		public Func<KeyInput, bool> OnTabKey = _ => false;
		public Func<KeyInput, bool> OnEscKey = _ => false;
		public Func<bool> OnAltKey = () => false;
		public Action OnLoseFocus = () => { };
		public Action OnTextEdited = () => { };
		public int CursorPosition { get; set; }

		public Func<bool> IsValid = () => true;
		public string Font = ChromeMetrics.Get<string>("TextfieldFont");
		public Color TextColor = ChromeMetrics.Get<Color>("TextfieldColor");
		public Color TextColorDisabled = ChromeMetrics.Get<Color>("TextfieldColorDisabled");
		public Color TextColorInvalid = ChromeMetrics.Get<Color>("TextfieldColorInvalid");
		public Color TextColorHighlight = ChromeMetrics.Get<Color>("TextfieldColorHighlight");

		protected int selectionStartIndex = -1;
		protected int selectionEndIndex = -1;
		protected bool mouseSelectionActive = false;

		public TextFieldWidget() { }

		protected TextFieldWidget(TextFieldWidget widget)
			: base(widget)
		{
			Text = widget.Text;
			MaxLength = widget.MaxLength;
			LeftMargin = widget.LeftMargin;
			RightMargin = widget.RightMargin;
			Type = widget.Type;
			Font = widget.Font;
			TextColor = widget.TextColor;
			TextColorDisabled = widget.TextColorDisabled;
			TextColorInvalid = widget.TextColorInvalid;
			TextColorHighlight = widget.TextColorHighlight;
			VisualHeight = widget.VisualHeight;
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

			if (mouseSelectionActive)
			{
				if (mi.Event == MouseInputEvent.Up)
				{
					mouseSelectionActive = false;
					return true;
				}
				else if (mi.Event != MouseInputEvent.Move)
					return false;
			}
			else if (mi.Event != MouseInputEvent.Down)
				return false;

			// Attempt to take keyboard focus
			if (!RenderBounds.Contains(mi.Location) || !TakeKeyboardFocus())
				return false;

			mouseSelectionActive = true;

			ResetBlinkCycle();

			var cachedCursorPos = CursorPosition;
			CursorPosition = ClosestCursorPosition(mi.Location.X);

			if (mi.Modifiers.HasModifier(Modifiers.Shift) || (mi.Event == MouseInputEvent.Move && mouseSelectionActive))
				HandleSelectionUpdate(cachedCursorPos, CursorPosition);
			else
				ClearSelection();

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

		string RemoveInvalidCharacters(string input)
		{
			switch (Type)
			{
				case TextFieldType.Filename:
				{
					var invalidIndex = -1;
					var invalidChars = Path.GetInvalidFileNameChars();
					while ((invalidIndex = input.IndexOfAny(invalidChars)) != -1)
						input = input.Remove(invalidIndex, 1);

					return input;
				}

				case TextFieldType.Integer:
					return new string(input.Where(c => char.IsDigit(c)).ToArray());

				default:
					return input;
			}
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (IsDisabled() || e.Event == KeyInputEvent.Up)
				return false;

			// Only take input if we are focused
			if (!HasKeyboardFocus)
				return false;

			var isOSX = Platform.CurrentPlatform == PlatformType.OSX;

			switch (e.Key)
			{
				case Keycode.RETURN:
				case Keycode.KP_ENTER:
					if (OnEnterKey(e))
						return true;
					break;

				case Keycode.TAB:
					if (OnTabKey(e))
						return true;
					break;

				case Keycode.ESCAPE:
					ClearSelection();
					if (OnEscKey(e))
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
						var cachedCurrentCursorPos = CursorPosition;

						if ((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Alt)))
							CursorPosition = GetPrevWhitespaceIndex();
						else if (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))
							CursorPosition = 0;
						else
							CursorPosition--;

						if (e.Modifiers.HasModifier(Modifiers.Shift))
							HandleSelectionUpdate(cachedCurrentCursorPos, CursorPosition);
						else
							ClearSelection();
					}

					break;

				case Keycode.RIGHT:
					ResetBlinkCycle();
					if (CursorPosition <= Text.Length - 1)
					{
						var cachedCurrentCursorPos = CursorPosition;

						if ((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Alt)))
							CursorPosition = GetNextWhitespaceIndex();
						else if (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))
							CursorPosition = Text.Length;
						else
							CursorPosition++;

						if (e.Modifiers.HasModifier(Modifiers.Shift))
							HandleSelectionUpdate(cachedCurrentCursorPos, CursorPosition);
						else
							ClearSelection();
					}

					break;

				case Keycode.HOME:
					ResetBlinkCycle();
					if (e.Modifiers.HasModifier(Modifiers.Shift))
						HandleSelectionUpdate(CursorPosition, 0);
					else
						ClearSelection();

					CursorPosition = 0;
					break;

				case Keycode.END:
					ResetBlinkCycle();

					if (e.Modifiers.HasModifier(Modifiers.Shift))
						HandleSelectionUpdate(CursorPosition, Text.Length);
					else
						ClearSelection();

					CursorPosition = Text.Length;
					break;

				case Keycode.D:
					if (e.Modifiers.HasModifier(Modifiers.Ctrl) && CursorPosition < Text.Length)
					{
						// Write directly to the Text backing field to avoid unnecessary validation
						text = text.Remove(CursorPosition, 1);
						CursorPosition = CursorPosition.Clamp(0, text.Length);

						OnTextEdited();
					}

					break;

				case Keycode.K:
					// ctrl+k is equivalent to cmd+delete on osx (but also works on osx)
					ResetBlinkCycle();
					if (e.Modifiers.HasModifier(Modifiers.Ctrl) && CursorPosition < Text.Length)
					{
						// Write directly to the Text backing field to avoid unnecessary validation
						text = text.Remove(CursorPosition);
						CursorPosition = CursorPosition.Clamp(0, text.Length);

						OnTextEdited();
					}

					break;

				case Keycode.U:
					// ctrl+u is equivalent to cmd+backspace on osx
					ResetBlinkCycle();
					if (!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl) && CursorPosition > 0)
					{
						// Write directly to the Text backing field to avoid unnecessary validation
						text = text.Substring(CursorPosition);
						CursorPosition = 0;
						ClearSelection();
						OnTextEdited();
					}

					break;

				case Keycode.X:
					ResetBlinkCycle();
					if (((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))) &&
						!string.IsNullOrEmpty(Text) && selectionStartIndex != -1)
					{
						var lowestIndex = selectionStartIndex < selectionEndIndex ? selectionStartIndex : selectionEndIndex;
						var highestIndex = selectionStartIndex < selectionEndIndex ? selectionEndIndex : selectionStartIndex;
						Game.Renderer.SetClipboardText(Text.Substring(lowestIndex, highestIndex - lowestIndex));

						RemoveSelectedText();
					}

					break;
				case Keycode.C:
					ResetBlinkCycle();
					if (((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Meta)))
						&& !string.IsNullOrEmpty(Text) && selectionStartIndex != -1)
					{
						var lowestIndex = selectionStartIndex < selectionEndIndex ? selectionStartIndex : selectionEndIndex;
						var highestIndex = selectionStartIndex < selectionEndIndex ? selectionEndIndex : selectionStartIndex;
						Game.Renderer.SetClipboardText(Text.Substring(lowestIndex, highestIndex - lowestIndex));
					}

					break;

				case Keycode.DELETE:
					// cmd+delete is equivalent to ctrl+k on non-osx
					ResetBlinkCycle();
					if (selectionStartIndex != -1)
						RemoveSelectedText();
					else if (CursorPosition < Text.Length)
					{
						// Write directly to the Text backing field to avoid unnecessary validation
						if ((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Alt)))
							text = text.Substring(0, CursorPosition) + text.Substring(GetNextWhitespaceIndex());
						else if (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))
							text = text.Remove(CursorPosition);
						else
							text = text.Remove(CursorPosition, 1);

						CursorPosition = CursorPosition.Clamp(0, text.Length);
						OnTextEdited();
					}

					break;

				case Keycode.BACKSPACE:
					// cmd+backspace is equivalent to ctrl+u on non-osx
					ResetBlinkCycle();
					if (selectionStartIndex != -1)
						RemoveSelectedText();
					else if (CursorPosition > 0)
					{
						// Write directly to the Text backing field to avoid unnecessary validation
						if ((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Alt)))
						{
							var prevWhitespace = GetPrevWhitespaceIndex();
							text = text.Substring(0, prevWhitespace) + text.Substring(CursorPosition);
							CursorPosition = prevWhitespace;
						}
						else if (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))
						{
							text = text.Substring(CursorPosition);
							CursorPosition = 0;
						}
						else
						{
							CursorPosition--;
							text = text.Remove(CursorPosition, 1);
						}

						OnTextEdited();
					}

					break;

				case Keycode.V:
					ResetBlinkCycle();

					if (selectionStartIndex != -1)
						RemoveSelectedText();

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
				case Keycode.A:
					// Ctrl+A as Select-All, or Cmd+A on OSX
					if ((!isOSX && e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && e.Modifiers.HasModifier(Modifiers.Meta)))
					{
						ClearSelection();
						HandleSelectionUpdate(0, Text.Length);
					}

					break;
				default:
					break;
			}

			return true;
		}

		public override bool HandleTextInput(string input)
		{
			if (!HasKeyboardFocus || IsDisabled())
				return false;

			// Validate input
			input = RemoveInvalidCharacters(input);
			if (input.Length == 0)
				return true;

			if (selectionStartIndex != -1)
				RemoveSelectedText();

			if (MaxLength > 0 && Text.Length >= MaxLength)
				return true;

			var pasteLength = input.Length;

			// Truncate the pasted string if the total length (current + paste) is greater than the maximum.
			if (MaxLength > 0 && MaxLength > Text.Length)
				pasteLength = Math.Min(input.Length, MaxLength - Text.Length);

			// Write directly to the Text backing field to avoid repeating the invalid character validation
			text = text.Insert(CursorPosition, input.Substring(0, pasteLength));
			CursorPosition += pasteLength;
			ClearSelection();
			OnTextEdited();

			return true;
		}

		void HandleSelectionUpdate(int prevCursorPos, int newCursorPos)
		{
			// If selection index is -1, there's no selection already open so create one
			if (selectionStartIndex == -1)
				selectionStartIndex = prevCursorPos;

			selectionEndIndex = newCursorPos;

			if (selectionStartIndex == selectionEndIndex)
				ClearSelection();
		}

		void ClearSelection()
		{
			selectionStartIndex = -1;
			selectionEndIndex = -1;
		}

		void RemoveSelectedText()
		{
			if (selectionStartIndex != -1)
			{
				var lowestIndex = selectionStartIndex < selectionEndIndex ? selectionStartIndex : selectionEndIndex;
				var highestIndex = selectionStartIndex < selectionEndIndex ? selectionEndIndex : selectionStartIndex;

				// Write directly to the Text backing field to avoid unnecessary validation
				text = text.Remove(lowestIndex, highestIndex - lowestIndex);

				ClearSelection();

				CursorPosition = lowestIndex;
				OnTextEdited();
			}
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
			var hover = Ui.MouseOverWidget == this || Children.Any(c => c == Ui.MouseOverWidget);
			var state = WidgetUtils.GetStatefulImageName(Background, disabled, false, hover, HasKeyboardFocus);

			WidgetUtils.DrawPanel(state,
				new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height));

			// Inset text by the margin and center vertically
			var verticalMargin = (Bounds.Height - textSize.Y) / 2 - VisualHeight;
			var textPos = pos + new int2(LeftMargin, verticalMargin);

			// Right align when editing and scissor when the text overflows
			var isTextOverflowing = textSize.X > Bounds.Width - LeftMargin - RightMargin;
			if (isTextOverflowing)
			{
				if (HasKeyboardFocus)
					textPos += new int2(Bounds.Width - LeftMargin - RightMargin - textSize.X, 0);

				Game.Renderer.EnableScissor(new Rectangle(pos.X + LeftMargin, pos.Y,
					Bounds.Width - LeftMargin - RightMargin, Bounds.Bottom));
			}

			// Draw the highlight around the selected area
			if (selectionStartIndex != -1)
			{
				var visualSelectionStartIndex = selectionStartIndex < selectionEndIndex ? selectionStartIndex : selectionEndIndex;
				var visualSelectionEndIndex = selectionStartIndex < selectionEndIndex ? selectionEndIndex : selectionStartIndex;
				var highlightStartX = font.Measure(apparentText.Substring(0, visualSelectionStartIndex)).X;
				var highlightEndX = font.Measure(apparentText.Substring(0, visualSelectionEndIndex)).X;

				WidgetUtils.FillRectWithColor(
					new Rectangle(textPos.X + highlightStartX, textPos.Y, highlightEndX - highlightStartX, Bounds.Height - (verticalMargin * 2)), TextColorHighlight);
			}

			var color =
				disabled ? TextColorDisabled
				: IsValid() ? TextColor
				: TextColorInvalid;
			font.DrawText(apparentText, textPos, color);

			if (showCursor && HasKeyboardFocus)
				font.DrawText("|", new float2(textPos.X + cursorPosition.X - 2, textPos.Y), TextColor);

			if (isTextOverflowing)
				Game.Renderer.DisableScissor();
		}

		public override Widget Clone() { return new TextFieldWidget(this); }
	}
}
