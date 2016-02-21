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

			if ((d.ContainsKey("Length") && d["Length"].Value == "*") || (d.ContainsKey("End") && d["End"].Value == "*"))
				Length = Frames.Length - Start;
			else if (d.ContainsKey("Length"))
				Length = Exts.ParseIntegerInvariant(d["Length"].Value);
			else if (d.ContainsKey("End"))
				Length = Exts.ParseIntegerInvariant(d["End"].Value) - Start;
			else
				Length = 1;

			Frames = cache[cursorSrc]
				.Skip(Start)
				.Take(Length)
				.ToArray();

			if (d.ContainsKey("X"))
			{
				int x;
				Exts.TryParseIntegerInvariant(d["X"].Value, out x);
				Hotspot = Hotspot.WithX(x);
			}

			if (d.ContainsKey("Y"))
			{
				int y;
				Exts.TryParseIntegerInvariant(d["Y"].Value, out y);
				Hotspot = Hotspot.WithY(y);
			}
		}
	}
}
