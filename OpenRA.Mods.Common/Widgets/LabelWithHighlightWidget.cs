#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class LabelWithHighlightWidget : LabelWidget
	{
		public Color HighlightColor = ChromeMetrics.Get<Color>("TextHighlightColor");
		readonly CachedTransform<string, (string Text, bool Highlighted)[]> textComponents;

		[ObjectCreator.UseCtor]
		public LabelWithHighlightWidget()
			: base()
		{
			textComponents = new CachedTransform<string, (string, bool)[]>(MakeComponents);
		}

		protected LabelWithHighlightWidget(LabelWithHighlightWidget other)
			: base(other)
		{
			HighlightColor = other.HighlightColor;
			textComponents = new CachedTransform<string, (string, bool)[]>(MakeComponents);
		}

		static (string Text, bool Highlighted)[] MakeComponents(string text)
		{
			var components = new List<(string, bool)>();
			foreach (var l in text.Split(new[] { "\\n" }, StringSplitOptions.None))
			{
				var line = l;

				while (line.Length > 0)
				{
					var highlightStart = line.IndexOf('{');
					var highlightEnd = line.IndexOf('}', 0);

					if (highlightStart > 0 && highlightEnd > highlightStart)
					{
						// Normal line segment before highlight
						var lineNormal = line.Substring(0, highlightStart);
						components.Add((lineNormal, false));

						// Highlight line segment
						var lineHighlight = line.Substring(highlightStart + 1, highlightEnd - highlightStart - 1);
						components.Add((lineHighlight, true));
						line = line.Substring(highlightEnd + 1);
					}
					else
					{
						// Final normal line segment
						components.Add((line, false));
						break;
					}
				}
			}

			return components.ToArray();
		}

		protected override void DrawInner(string text, SpriteFont font, Color color, int2 position)
		{
			var advance = 0;
			foreach (var c in textComponents.Update(text))
			{
				base.DrawInner(c.Text, font, c.Highlighted ? HighlightColor : color, position + new int2(advance, 0));
				advance += font.Measure(c.Text).X;
			}
		}

		public override Widget Clone() { return new LabelWithHighlightWidget(this); }

		public static int MeasureWidth(string text, SpriteFont font)
		{
			var width = 0;
			foreach (var c in MakeComponents(text))
				width += font.Measure(c.Text).X;

			return width;
		}

		public static string TruncateText(string text, int width, SpriteFont font)
		{
			var components = MakeComponents(text);
			var accumulatedWidth = new int[components.Length];
			accumulatedWidth[0] = font.Measure(components[0].Text).X;
			for (var i = 1; i < components.Length; i++)
				accumulatedWidth[i] = accumulatedWidth[i - 1] + font.Measure(components[i].Text).X;

			// Is the text already short enough?
			if (accumulatedWidth[components.Length - 1] <= width)
				return text;

			// Work backwards until we find the component that straddles the break
			for (var i = components.Length - 1; i >= 1; i--)
			{
				var offset = accumulatedWidth[i - 1];
				if (offset > width)
					continue;

				var trimmed = components[i].Text + "...";
				var trimmedWidth = font.Measure(trimmed).X;
				while (offset + trimmedWidth > width && trimmed.Length > 3)
				{
					trimmed = components[i].Text.Substring(0, trimmed.Length - 4) + "...";
					trimmedWidth = font.Measure(trimmed).X;
				}

				// Component is too close to the limit for even "..." to fit
				if (trimmed.Length == 3)
					continue;

				// Rebuild the unformatted string
				var result = new StringBuilder();
				for (var j = 0; j <= i; j++)
				{
					var c = components[j];
					if (c.Highlighted)
						result.Append("{");

					if (j < i)
						result.Append(c.Text);
					else
						result.Append(trimmed);

					if (c.Highlighted)
						result.Append("}");
				}

				return result.ToString();
			}

			return string.Empty;
		}
	}
}
