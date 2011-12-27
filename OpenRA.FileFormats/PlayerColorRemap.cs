#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRA.FileFormats
{
	// TODO: ship this out of here.
	public enum PaletteFormat { ra, cnc, d2k }

	public class PlayerColorRemap : IPaletteRemap
	{
		Dictionary<int, Color> remapColors;

		static readonly int[] CncRemapRamp = new[] { 0, 2, 4, 6, 8, 10, 13, 15, 1, 3, 5, 7, 9, 11, 12, 14 };
		static readonly int[] NormalRemapRamp = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

		static int GetRemapBase(PaletteFormat fmt)
		{
			return (fmt == PaletteFormat.cnc) ? 0xb0 : (fmt == PaletteFormat.d2k) ? 240 : 80;
		}

		static int[] GetRemapRamp(PaletteFormat fmt)
		{
			return (fmt == PaletteFormat.cnc) ? CncRemapRamp : NormalRemapRamp;
		}

		public static int GetRemapIndex(PaletteFormat fmt, int i)
		{
			return GetRemapBase(fmt) + GetRemapRamp(fmt)[i];
		}

		public PlayerColorRemap(PaletteFormat fmt, ColorRamp c)
		{
			var c1 = c.GetColor(0);
			var c2 = c.GetColor(1); /* temptemp: this can be expressed better */

			var baseIndex = GetRemapBase(fmt);
			var ramp = GetRemapRamp(fmt);

			remapColors = ramp.Select((x, i) => Pair.New(baseIndex + i, Exts.ColorLerp(x / 16f, c1, c2)))
				.ToDictionary(u => u.First, u => u.Second);
		}

		public Color GetRemappedColor(Color original, int index)
		{
			Color c;
			return remapColors.TryGetValue(index, out c)
				? c : original;
		}
	}
}
