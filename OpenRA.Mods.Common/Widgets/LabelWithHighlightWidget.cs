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
	public class LabelWithHighlightWidget : LabelWidget
	{
		public Color HighlightColor = ChromeMetrics.Get<Color>("TextHighlightColor");

		[ObjectCreator.UseCtor]
		public LabelWithHighlightWidget(ModData modData)
			: base(modData) { }

		protected LabelWithHighlightWidget(LabelWithHighlightWidget other)
			: base(other)
		{
			HighlightColor = other.HighlightColor;
		}

		static (string Text, bool Highlighted)[] ParseLineComponents(string line)
		{
			var components = new List<(string, bool)>();

			while (line.Length > 0)
			{
				var highlightStart = line.IndexOf('<');
				if (highlightStart == -1)
				{
					components.Add((line, false));
					break;
				}

				var highlightEnd = line.IndexOf('>', highlightStart);
				if (highlightEnd > highlightStart)
				{
					if (highlightStart > 0)
						components.Add((line[..highlightStart], false));

					components.Add((line[(highlightStart + 1)..highlightEnd], true));
					line = line[(highlightEnd + 1)..];
				}
				else
				{
					components.Add((line[..(highlightStart + 1)], false));
					line = line[(highlightStart + 1)..];
				}
			}

			return components.ToArray();
		}

		protected override void DrawInner(string text, SpriteFont font, Color color, int2 position)
		{
			var normalized = text.Replace("\\n", "\n", StringComparison.Ordinal);
			var lineHeight = font.Measure("A").Y;
			var y = 0;

			foreach (var line in normalized.Split('\n', StringSplitOptions.None))
			{
				var x = 0;
				foreach (var c in ParseLineComponents(line))
				{
					base.DrawInner(c.Text, font, c.Highlighted ? HighlightColor : color, position + new int2(x, y));
					x += font.Measure(c.Text).X;
				}

				y += lineHeight;
			}
		}

		public override LabelWithHighlightWidget Clone() { return new LabelWithHighlightWidget(this); }
	}
}
