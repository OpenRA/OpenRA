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

using System.Linq;

namespace OpenRA.Graphics
{
	public class CursorSequence
	{
		public readonly string Name;
		public readonly int Start;
		public readonly int Length;
		public readonly string Palette;
		public readonly int2 Hotspot;

		public readonly ISpriteFrame[] Frames;

		public CursorSequence(FrameCache cache, string name, string cursorSrc, string palette, MiniYaml info)
		{
			var d = info.ToDictionary();

			Start = Exts.ParseIntegerInvariant(d["Start"].Value);
			Palette = palette;
			Name = name;

			var cursorSprites = cache[cursorSrc];
			Frames = cursorSprites.Skip(Start).ToArray();

			if ((d.TryGetValue("Length", out var yaml) && yaml.Value == "*") ||
				(d.TryGetValue("End", out yaml) && yaml.Value == "*"))
				Length = Frames.Length;
			else if (d.TryGetValue("Length", out yaml))
				Length = Exts.ParseIntegerInvariant(yaml.Value);
			else if (d.TryGetValue("End", out yaml))
				Length = Exts.ParseIntegerInvariant(yaml.Value) - Start;
			else
				Length = 1;

			Frames = Frames.Take(Length).ToArray();

			if (Start > cursorSprites.Length)
				throw new YamlException($"Cursor {name}: {nameof(Start)} is greater than the length of the sprite sequence.");

			if (Length > cursorSprites.Length)
				throw new YamlException($"Cursor {name}: {nameof(Length)} is greater than the length of the sprite sequence.");

			if (d.TryGetValue("X", out yaml))
			{
				Exts.TryParseIntegerInvariant(yaml.Value, out var x);
				Hotspot = Hotspot.WithX(x);
			}

			if (d.TryGetValue("Y", out yaml))
			{
				Exts.TryParseIntegerInvariant(yaml.Value, out var y);
				Hotspot = Hotspot.WithY(y);
			}
		}
	}
}
