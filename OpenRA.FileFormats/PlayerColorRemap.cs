#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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

		public PlayerColorRemap(Color c1, Color c2, bool useSplitRamp)
		{
			var baseIndex = useSplitRamp ? 0xb0 : 80;
			var ramp = useSplitRamp
				? new[] { 0, 2, 4, 6, 8, 10, 13, 15, 1, 3, 5, 7, 9, 11, 12, 14 }
				: new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

			remapColors = ramp.Select((x, i) => Pair.New(baseIndex + i, ColorLerp(x / 16f, c1, c2)))
				.ToDictionary(u => u.First, u => u.Second);
		}

		static Color ColorLerp(float t, Color c1, Color c2)
		{
			return Color.FromArgb(255,
				(int)(t * c2.R + (1 - t) * c1.R),
				(int)(t * c2.G + (1 - t) * c1.G),
				(int)(t * c2.B + (1 - t) * c1.B));
		}
		
		public Color GetRemappedColor(Color original, int index)
		{
			Color c;
			return remapColors.TryGetValue(index, out c) 
				? c : original;
		}
	}
}
