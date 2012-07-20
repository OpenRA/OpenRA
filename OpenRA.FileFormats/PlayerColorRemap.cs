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

	public class PlayerColorRemap : IPaletteRemap
	{
		Dictionary<int, Color> remapColors;

		public static int GetRemapIndex(int[] Ramp, int i)
		{
			return Ramp[i];
		}

		public PlayerColorRemap(int[] Ramp, ColorRamp c)
		{
			var c1 = c.GetColor(0);
			var c2 = c.GetColor(1); // temptemp: this can be expressed better

			var baseIndex = Ramp[0];
			var RemapRamp = Ramp.Select(r => r - Ramp[0]).ToArray();

			if (Ramp[0] > Ramp[15]) // reversed remapping
                        {
				baseIndex = Ramp[15];
				for (int i=15; i>0; i--)
					RemapRamp = Ramp.Select(r => r - Ramp[15]).ToArray();
			}
			
			remapColors = RemapRamp.Select((x, i) => Pair.New(baseIndex + i, Exts.ColorLerp(x / 16f, c1, c2)))
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
