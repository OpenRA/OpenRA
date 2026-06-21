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
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class LabelWithSelectionWidget : LabelWidget
	{
		public readonly Color TextSelectionBackgroundColor = ChromeMetrics.Get<Color>("TextfieldColorHighlight");
		public string Cursor = ChromeMetrics.Get<string>("DefaultCursor");

		int selectionStart = -1;
		int selectionEnd = -1;
		bool mouseSelecting;

		// Tracks the displayed text so we can clear the selection if the text changes between frames.
		string selectionText;

		// Cached highlight rectangles, stored relative to the text position so they stay valid while the chat
		// scrolls. They are only recalculated when the selection changes, not every frame.
		readonly List<Rectangle> selectionRects = [];
		int cachedSelectionStart = -1;
		int cachedSelectionEnd = -1;

		[ObjectCreator.UseCtor]
		public LabelWithSelectionWidget(ModData modData)
			: base(modData) { }

		protected LabelWithSelectionWidget(LabelWithSelectionWidget other)
			: base(other)
		{
			TextSelectionBackgroundColor = other.TextSelectionBackgroundColor;
			Cursor = other.Cursor;
		}

		public override void Draw()
		{
			if (!TryGetTextLayout(out var text, out var font, out var position))
				return;

			if (text != selectionText)
			{
				ClearSelection();
				selectionText = text;
			}

			DrawInner(text, font, GetColor(), position);
		}

		protected override void DrawInner(string text, SpriteFont font, Color color, int2 position)
		{
			if (selectionStart != -1 && selectionStart != selectionEnd)
			{
				if (selectionStart != cachedSelectionStart || selectionEnd != cachedSelectionEnd)
				{
					cachedSelectionStart = selectionStart;
					cachedSelectionEnd = selectionEnd;
					RecalculateSelectionRects(text, font);
				}

				// Draw one highlight rectangle per line, then render text normally on top.
				foreach (var r in selectionRects)
					WidgetUtils.FillRectWithColor(
						new Rectangle(position.X + r.X, position.Y + r.Y, r.Width, r.Height),
						TextSelectionBackgroundColor);
			}

			base.DrawInner(text, font, color, position);
		}

		// Computes the highlight rectangles once per selection change rather than every frame. The rectangles are
		// stored relative to the text position so they remain correct as the chat scrolls.
		void RecalculateSelectionRects(string text, SpriteFont font)
		{
			selectionRects.Clear();

			var start = Math.Min(selectionStart, selectionEnd);
			var end = Math.Max(selectionStart, selectionEnd);

			var lines = text.Split('\n');
			var lineHeight = font.Measure(" ").Y;
			var height = lineHeight + 2 * font.TopOffset - 2;
			var charOffset = 0;
			for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
			{
				var line = lines[lineIndex];
				var lineStart = charOffset;
				var lineEnd = charOffset + line.Length;

				var selStart = AlignToTextElement(line, Math.Max(start, lineStart) - lineStart);
				var selEnd = AlignToTextElement(line, Math.Min(end, lineEnd) - lineStart);

				if (selStart < selEnd)
				{
					var x = font.Measure(line[..selStart]).X;
					var y = lineIndex * lineHeight + 1;
					var w = font.Measure(line[selStart..selEnd]).X;
					selectionRects.Add(new Rectangle(x, y, w, height));
				}

				charOffset += line.Length + 1;
			}
		}

		// Clears the selection together with the cached highlight rectangles so no stale highlight survives.
		void ClearSelection()
		{
			selectionStart = -1;
			selectionEnd = -1;
			selectionRects.Clear();
			cachedSelectionStart = -1;
			cachedSelectionEnd = -1;
		}

		static int NextTextElementIndex(string s, int i)
		{
			return char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]) ? i + 2 : i + 1;
		}

		static int AlignToTextElement(string s, int i)
		{
			return i > 0 && i < s.Length && char.IsLowSurrogate(s[i]) ? i - 1 : i;
		}

		static (int Start, int End) GetWordBounds(string text, int charIndex)
		{
			var start = charIndex;
			while (start > 0 && char.IsLetterOrDigit(text[AlignToTextElement(text, start - 1)]))
				start = AlignToTextElement(text, start - 1);

			var end = charIndex;
			while (end < text.Length && char.IsLetterOrDigit(text[end]))
				end = NextTextElementIndex(text, end);

			return (start, end);
		}

		static (int Start, int End) GetLineBounds(string text, int charIndex)
		{
			var lineStart = text.LastIndexOf('\n', Math.Max(0, charIndex - 1)) + 1;
			var lineEnd = text.IndexOf('\n', charIndex);
			return (lineStart, lineEnd == -1 ? text.Length : lineEnd);
		}

		int GetCharIndexAtPosition(int2 location)
		{
			if (!TryGetTextLayout(out var text, out var font, out var textPosition))
				return 0;

			var lines = text.Split('\n');
			var lineHeight = font.Measure(" ").Y;
			var relY = location.Y - textPosition.Y;
			var lineIndex = Math.Max(0, Math.Min(lines.Length - 1, relY / lineHeight));

			var charOffset = 0;
			for (var i = 0; i < lineIndex; i++)
				charOffset += lines[i].Length + 1;

			var line = lines[lineIndex];
			var relX = location.X - textPosition.X;

			var currentWidth = 0;
			for (var i = 0; i < line.Length;)
			{
				var next = NextTextElementIndex(line, i);
				var nextWidth = font.Measure(line[..next]).X;
				if (relX < (currentWidth + nextWidth) / 2)
					return charOffset + i;
				currentWidth = nextWidth;
				i = next;
			}

			return charOffset + line.Length;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			switch (mi.Event)
			{
				case MouseInputEvent.Down:
					if (!RenderBounds.Contains(mi.Location))
					{
						if (HasMouseFocus)
						{
							ClearSelection();
							mouseSelecting = false;
							YieldMouseFocus(mi);
							YieldKeyboardFocus();
						}

						return false;
					}

					TakeMouseFocus(mi);

					if (selectionText != null && mi.MultiTapCount >= 3)
					{
						var (lineStart, lineEnd) = GetLineBounds(selectionText, GetCharIndexAtPosition(mi.Location));
						selectionStart = lineStart;
						selectionEnd = lineEnd;
						mouseSelecting = false;
						TakeKeyboardFocus();
					}
					else if (selectionText != null && mi.MultiTapCount == 2)
					{
						var clickIndex = GetCharIndexAtPosition(mi.Location);
						var (wordStart, wordEnd) = GetWordBounds(selectionText, clickIndex);
						selectionStart = wordStart;
						selectionEnd = wordEnd;
						mouseSelecting = wordStart == wordEnd;
						if (wordStart != wordEnd)
							TakeKeyboardFocus();
					}
					else
					{
						selectionStart = GetCharIndexAtPosition(mi.Location);
						selectionEnd = selectionStart;
						mouseSelecting = true;
					}

					return true;

				case MouseInputEvent.Move:
					if (mouseSelecting)
					{
						selectionEnd = GetCharIndexAtPosition(mi.Location);
						return true;
					}

					return false;

				case MouseInputEvent.Up:
					if (mouseSelecting)
					{
						mouseSelecting = false;
						selectionEnd = GetCharIndexAtPosition(mi.Location);
						if (selectionStart == selectionEnd)
							ClearSelection();
						else
							TakeKeyboardFocus();

						return true;
					}

					return false;
			}

			return false;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up)
				return false;

			if (selectionStart == -1 || selectionStart == selectionEnd || selectionText == null)
				return false;

			var isOSX = Platform.CurrentPlatform == PlatformType.OSX;
			var copyModifier = isOSX ? Modifiers.Meta : Modifiers.Ctrl;

			if (!e.Modifiers.HasModifier(copyModifier) || e.Key != Keycode.C)
				return false;

			var start = AlignToTextElement(selectionText, Math.Min(selectionStart, selectionEnd));
			var end = AlignToTextElement(selectionText, Math.Max(selectionStart, selectionEnd));
			var selected = selectionText[start..end];
			if (!string.IsNullOrEmpty(selected))
				Game.Renderer.SetClipboardText(selected);

			return true;
		}

		public override string GetCursor(int2 pos) { return Cursor; }

		public override LabelWithSelectionWidget Clone() { return new LabelWithSelectionWidget(this); }
	}
}
