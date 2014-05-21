#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;

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

		public CursorSequence(SpriteLoader loader, string cursorSrc, string palette, MiniYaml info)
		{
			sprites = loader.LoadAllSprites(cursorSrc);
			var d = info.GetNodesDict();

			start = Exts.ParseIntegerInvariant(d["start"].Value);
			this.palette = palette;

			if ((d.ContainsKey("length") && d["length"].Value == "*") || (d.ContainsKey("end") && d["end"].Value == "*"))
				length = sprites.Length - start;
			else if (d.ContainsKey("length"))
				length = Exts.ParseIntegerInvariant(d["length"].Value);
			else if (d.ContainsKey("end"))
				length = Exts.ParseIntegerInvariant(d["end"].Value) - start;
			else
				length = 1;

			if (d.ContainsKey("x"))
				Exts.TryParseIntegerInvariant(d["x"].Value, out Hotspot.X);
			if (d.ContainsKey("y"))
				Exts.TryParseIntegerInvariant(d["y"].Value, out Hotspot.Y);
		}

		public Sprite GetSprite(int frame)
		{
			return sprites[(frame % length) + start];
		}
	}
}
