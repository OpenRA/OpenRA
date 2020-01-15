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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class LabelWithHighlightWidget : LabelWidget
	{
		public Color HighlightColor = ChromeMetrics.Get<Color>("TextHighlightColor");
		readonly CachedTransform<string, Pair<string, bool>[]> textComponents;

		[ObjectCreator.UseCtor]
		public LabelWithHighlightWidget()
			: base()
		{
			textComponents = new CachedTransform<string, Pair<string, bool>[]>(MakeComponents);
		}

		protected LabelWithHighlightWidget(LabelWithHighlightWidget other)
			: base(other)
		{
			HighlightColor = other.HighlightColor;
			textComponents = new CachedTransform<string, Pair<string, bool>[]>(MakeComponents);
		}

		Pair<string, bool>[] MakeComponents(string text)
		{
			List<Pair<string, bool>> components = new List<Pair<string, bool>>();
			foreach (var l in text.Split(new[] { "\\n" }, StringSplitOptions.None))
			{
				var line = l;

				while (line.Length > 0)
				{
					var highlightStart = line.IndexOf('{');
					var highlightEnd = line.IndexOf('}', 0);

					if (highlightStart > 0 && highlightEnd > highlightStart)
					{
						if (highlightStart > 0)
						{
							// Normal line segment before highlight
							var lineNormal = line.Substring(0, highlightStart);
							components.Add(Pair.New(lineNormal, false));
						}

						// Highlight line segment
						var lineHighlight = line.Substring(highlightStart + 1, highlightEnd - highlightStart - 1);
						components.Add(Pair.New(lineHighlight, true));
						line = line.Substring(highlightEnd + 1);
					}
					else
					{
						// Final normal line segment
						components.Add(Pair.New(line, false));
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
				base.DrawInner(c.First, font, c.Second ? HighlightColor : color, position + new int2(advance, 0));
				advance += font.Measure(c.First).X;
			}
		}

		public override Widget Clone() { return new LabelWithHighlightWidget(this); }
	}
}
