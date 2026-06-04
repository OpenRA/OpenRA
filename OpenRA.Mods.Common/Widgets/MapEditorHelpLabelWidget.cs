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
	/// <summary>
	/// Help text renderer. Section intros under blue titles use <see cref="SectionIntroFont"/>
	/// (FreeSans Oblique — italic/cursive style, same typeface family as <see cref="DescriptionFont"/>).
	/// </summary>
	public sealed class MapEditorHelpLabelWidget : LabelWidget
	{
		enum SegmentKind { Text, Key, Title, Description, SectionIntro }

		readonly struct Segment
		{
			public readonly string Text;
			public readonly SegmentKind Kind;

			public Segment(string text, SegmentKind kind)
			{
				Text = text;
				Kind = kind;
			}
		}

		public Color KeyColor = Color.FromArgb(0, 255, 0);
		public Color TitleColor = Color.FromArgb(120, 180, 255);
		public string DescriptionFont = "Small";
		public string SectionIntroFont = "SmallItalic";
		public int DescriptionColumnX;

		[ObjectCreator.UseCtor]
		public MapEditorHelpLabelWidget(ModData modData)
			: base(modData) { }

		MapEditorHelpLabelWidget(MapEditorHelpLabelWidget other)
			: base(other)
		{
			KeyColor = other.KeyColor;
			TitleColor = other.TitleColor;
			DescriptionFont = other.DescriptionFont;
			SectionIntroFont = other.SectionIntroFont;
			DescriptionColumnX = other.DescriptionColumnX;
		}

		public static int MeasureHelpTextHeight(string text, SpriteFont bodyFont, SpriteFont descFont, SpriteFont introFont)
		{
			var normalized = Sanitize(text).Replace("\\n", "\n", StringComparison.Ordinal);
			var height = 0;

			foreach (var line in normalized.Split('\n', StringSplitOptions.None))
			{
				var lineHeight = bodyFont.Measure("Ay").Y;

				foreach (var segment in ParseLine(line))
				{
					var segmentFont = SegmentFont(segment.Kind, bodyFont, descFont, introFont);
					lineHeight = Math.Max(lineHeight, segmentFont.Measure("Ay").Y);
				}

				height += lineHeight;
			}

			return height;
		}

		protected override void DrawInner(string text, SpriteFont font, Color color, int2 position)
		{
			var normalized = Sanitize(text).Replace("\\n", "\n", StringComparison.Ordinal);
			var descFont = GetFont(DescriptionFont, font);
			var introFont = GetFont(SectionIntroFont, descFont);
			var y = 0;

			foreach (var line in normalized.Split('\n', StringSplitOptions.None))
			{
				var x = 0;
				var useDescColumn = DescriptionColumnX > 0 && (line.Contains('<') || line.StartsWith('|'));
				var lineHeight = font.Measure("Ay").Y;

				foreach (var segment in ParseLine(line))
				{
					var segmentFont = SegmentFont(segment.Kind, font, descFont, introFont);
					lineHeight = Math.Max(lineHeight, segmentFont.Measure("Ay").Y);

					if (segment.Kind == SegmentKind.Description && useDescColumn)
						x = DescriptionColumnX;
					else if (segment.Kind == SegmentKind.Key)
						x = 0;

					var segmentColor = SegmentColor(segment.Kind, color);
					base.DrawInner(segment.Text, segmentFont, segmentColor, position + new int2(x, y));
					x += segmentFont.Measure(segment.Text).X;
				}

				y += lineHeight;
			}
		}

		static SpriteFont SegmentFont(SegmentKind kind, SpriteFont bodyFont, SpriteFont descFont, SpriteFont introFont) => kind switch
		{
			SegmentKind.SectionIntro => introFont,
			SegmentKind.Description => descFont,
			_ => bodyFont,
		};

		SpriteFont GetFont(string name, SpriteFont fallback) =>
			!string.IsNullOrEmpty(name) && Game.Renderer.Fonts.TryGetValue(name, out var f) ? f : fallback;

		Color SegmentColor(SegmentKind kind, Color defaultColor) => kind switch
		{
			SegmentKind.Key => KeyColor,
			SegmentKind.Title => TitleColor,
			_ => defaultColor,
		};

		static string Sanitize(string text)
		{
			return text
				.Replace("\r\n", "\n", StringComparison.Ordinal)
				.Replace('\r', '\n')
				.Replace('\u2013', '-')
				.Replace('\u2014', '-');
		}

		static Segment[] ParseLine(string line)
		{
			if (line.Length == 0)
				return [new Segment("", SegmentKind.Text)];

			if (line.StartsWith('|') && line.EndsWith('~'))
				return [new Segment(line[2..^1], SegmentKind.Description)];

			if (line.StartsWith('['))
			{
				var end = line.IndexOf(']');
				if (end > 1)
					return [new Segment(line[1..end], SegmentKind.Title)];
			}

			if (line.StartsWith('%') && line.EndsWith('%') && line.Length > 2)
				return [new Segment(line[1..^1], SegmentKind.SectionIntro)];

			var segments = new List<Segment>();
			var remaining = line;

			while (remaining.Length > 0)
			{
				var keyStart = remaining.IndexOf('<');
				var descStart = remaining.IndexOf('~');

				if (keyStart == -1 && descStart == -1)
				{
					if (remaining.Length > 0)
						segments.Add(new Segment(remaining, SegmentKind.Text));
					break;
				}

				var next = MinIndex(keyStart, descStart);
				if (next > 0)
				{
					segments.Add(new Segment(remaining[..next], SegmentKind.Text));
					remaining = remaining[next..];
					continue;
				}

				if (remaining.StartsWith('<') && keyStart == 0)
				{
					var keyEnd = remaining.IndexOf('>');
					if (keyEnd > 1)
					{
						segments.Add(new Segment(remaining[1..keyEnd], SegmentKind.Key));
						remaining = remaining[(keyEnd + 1)..];
						continue;
					}
				}

				if (remaining.StartsWith('~') && descStart == 0)
				{
					var descEnd = remaining.IndexOf('~', 1);
					if (descEnd > 1)
					{
						segments.Add(new Segment(remaining[1..descEnd], SegmentKind.Description));
						remaining = remaining[(descEnd + 1)..];
						continue;
					}
				}

				segments.Add(new Segment(remaining[..1], SegmentKind.Text));
				remaining = remaining[1..];
			}

			return segments.ToArray();
		}

		static int MinIndex(int a, int b)
		{
			if (a < 0)
				return b;
			if (b < 0)
				return a;
			return Math.Min(a, b);
		}

		public override MapEditorHelpLabelWidget Clone() { return new MapEditorHelpLabelWidget(this); }
	}
}
