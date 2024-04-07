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
	public enum TextFieldType { General, Filename, Integer, UInteger, Float }
	public class TextFieldWidget : InputWidget
	{
		string text = string.Empty;
		public string Text
		{
			get => text;

			set
			{
				text = SanitizeInput(WhitelistChars(value));
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
				text = SanitizeInput(WhitelistChars(text));
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
				var dist = Math.Abs(start + font.Measure(apparentText[..i]).X - x);
				if (dist > minValue)
					break;
				minValue = dist;
				minIndex = i;
			}

			return minIndex;
		}

		int GetPrevWhitespaceIndex()
		{
			return Text[..CursorPosition].TrimEnd().LastIndexOf(' ') + 1;
		}

		int GetNextWhitespaceIndex()
		{
			var substr = Text[CursorPosition..];
			var substrTrimmed = substr.TrimStart();
			var trimmedSpaces = substr.Length - substrTrimmed.Length;
			var nextWhitespace = substrTrimmed.IndexOf(' ');
			if (nextWhitespace == -1)
				return Text.Length;

			return CursorPosition + trimmedSpaces + nextWhitespace;
		}

		string WhitelistChars(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			switch (Type)
			{
				case TextFieldType.General:
					return text;
				case TextFieldType.Filename:
					return new string(text.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
				case TextFieldType.Integer:
					return new string(text.Where(c => char.IsDigit(c) || (c == '-')).ToArray());
				case TextFieldType.UInteger:
					return new string(text.Where(c => char.IsDigit(c)).ToArray());
				case TextFieldType.Float:
					return new string(text.Where(c => char.IsDigit(c) || (c == '-') || c == '.').ToArray());
				default:
					throw new ArgumentOutOfRangeException(Type.ToString(), Type, "Invalid TextFieldType");
			}
		}

		string SanitizeInput(string text)
		{
			switch (Type)
			{
				case TextFieldType.General:
				case TextFieldType.Filename:
					return text;
				case TextFieldType.UInteger:
					return HandleEmptyText(RemoveZeros(text), out _);

				case TextFieldType.Integer:
					if (text.Length != 0)
					{
						if (text[0] == '-')
							text = '-' + text[1..].Replace("-", "");
						else
							text = text.Replace("-", "");
					}

					return HandleEmptyText(RemoveZeros(text), out _);

				case TextFieldType.Float:
					if (text.Length != 0)
					{
						if (text[0] == '-')
							text = '-' + text[1..].Replace("-", "");
						else
							text = text.Replace("-", "");

						var index = text.IndexOf('.');
						if (index != -1)
							text = text[..(index + 1)] + text[(index + 1)..].Replace(".", "");
					}

					return HandleEmptyText(RemoveZeros(text), out _);
				default:
					throw new ArgumentOutOfRangeException(Type.ToString(), Type, "Invalid TextFieldType");
			}
		}

		static string RemoveZeros(string text)
		{
			if (string.IsNullOrEmpty(text) || text.Length == 1)
				return text;

			var negative = text[0] == '-';
			var firstNonZero = -1;
			for (var i = negative ? 1 : 0; i < text.Length; i++)
			{
				if (text[i] != '0')
				{
					firstNonZero = i;
					break;
				}
			}

			if (firstNonZero == -1)
				return negative ? "-0" : "0";

			return negative ? '-' + text[firstNonZero..] : text[firstNonZero..];
		}

		public string HandleEmptyText(string text, out bool addedChar)
		{
			addedChar = false;
			switch (Type)
			{
				case TextFieldType.Integer:
				case TextFieldType.UInteger:
					if (string.IsNullOrEmpty(text))
					{
						addedChar = true;
						return "0";
					}

					if (text.Length == 1 && text[0] == '-')
					{
						addedChar = true;
						return "-0";
					}

					return text;
				case TextFieldType.Float:
					if (string.IsNullOrEmpty(text))
					{
						addedChar = true;
						return "0";
					}

					if (text.StartsWith('-'))
					{
						if (text.Length == 1)
						{
							addedChar = true;
							return "-0";
						}
						else if (text.Length > 1 && text[1] == '.')
						{
							addedChar = true;
							var remove = text.Length > 2 && text[2] == '0' ? 3 : 2;
							return "-0." + text[remove..];
						}
					}

					if (text.StartsWith('.'))
					{
						if (text.Length == 1)
						{
							addedChar = true;
							return "0.";
						}
						else
						{
							addedChar = true;
							return "0" + text;
						}
					}

					return text;
				default:
					return text;
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
						text = text[CursorPosition..];
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
						Game.Renderer.SetClipboardText(Text[lowestIndex..highestIndex]);

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
						Game.Renderer.SetClipboardText(Text[lowestIndex..highestIndex]);
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
							text = HandleEmptyText(text[..CursorPosition] + text[GetNextWhitespaceIndex()..], out var _);
						else if (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))
							text = HandleEmptyText(text.Remove(CursorPosition), out var _);
						else
							text = HandleEmptyText(text.Remove(CursorPosition, 1), out var _);

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
							text = HandleEmptyText(text[..prevWhitespace] + text[CursorPosition..], out var addedChar);
							CursorPosition = addedChar ? prevWhitespace + 1 : prevWhitespace;
						}
						else if (isOSX && e.Modifiers.HasModifier(Modifiers.Meta))
						{
							text = HandleEmptyText(text[CursorPosition..], out var _);
							CursorPosition = text.Length;
						}
						else
						{
							CursorPosition--;
							text = HandleEmptyText(text.Remove(CursorPosition, 1), out var _);
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
							clipboardText = clipboardText[..nl];

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
			if (!HasKeyboardFocus || IsDisabled() || string.IsNullOrEmpty(input))
				return true;

			var newText = SanitizeInput(text.Insert(CursorPosition, WhitelistChars(input)));
			if (string.IsNullOrEmpty(newText) || text == newText)
				return true;

			if (MaxLength > 0)
				newText = newText[..Math.Min(MaxLength, newText.Length)];

			if (text == newText)
				return true;

			if (selectionStartIndex != -1)
				RemoveSelectedText();

			CursorPosition = (CursorPosition + newText.Length - text.Length).Clamp(0, newText.Length);
			text = newText;
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
				text = HandleEmptyText(text.Remove(lowestIndex, highestIndex - lowestIndex), out var addedChar);

				ClearSelection();

				CursorPosition = addedChar ? lowestIndex + 1 : lowestIndex;
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
			var cursorPosition = font.Measure(apparentText[..CursorPosition]);

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
				var highlightStartX = font.Measure(apparentText[..visualSelectionStartIndex]).X;
				var highlightEndX = font.Measure(apparentText[..visualSelectionEndIndex]).X;

				WidgetUtils.FillRectWithColor(
					new Rectangle(textPos.X + highlightStartX, textPos.Y, highlightEndX - highlightStartX, Bounds.Height - verticalMargin * 2), TextColorHighlight);
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
