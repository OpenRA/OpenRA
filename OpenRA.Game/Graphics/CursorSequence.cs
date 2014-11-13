#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Graphics
{
	public class CursorSequence
	{
		readonly int start, length;
		readonly string palette;

		public int Start { get { return start; } }
		public int End { get { return start + length; } }
		public int Length { get { return length; } }
		public string Palette { get { return palette; } }
		public readonly int2 Hotspot;

		Sprite[] sprites;

		public CursorSequence(SpriteCache cache, string cursorSrc, string palette, MiniYaml info)
		{
			sprites = cache[cursorSrc];
			var d = info.ToDictionary();

			start = Exts.ParseIntegerInvariant(d["Start"].Value);
			this.palette = palette;

			if ((d.ContainsKey("Length") && d["Length"].Value == "*") || (d.ContainsKey("End") && d["End"].Value == "*"))
				length = sprites.Length - start;
			else if (d.ContainsKey("Length"))
				length = Exts.ParseIntegerInvariant(d["Length"].Value);
			else if (d.ContainsKey("End"))
				length = Exts.ParseIntegerInvariant(d["End"].Value) - start;
			else
				length = 1;

			if (d.ContainsKey("X"))
				Exts.TryParseIntegerInvariant(d["X"].Value, out Hotspot.X);
			if (d.ContainsKey("Y"))
				Exts.TryParseIntegerInvariant(d["Y"].Value, out Hotspot.Y);
		}

		public Sprite GetSprite(int frame)
		{
			return sprites[(frame % length) + start];
		}
	}
}
