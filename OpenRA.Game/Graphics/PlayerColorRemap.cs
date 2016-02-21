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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class PlayerColorRemap : IPaletteRemap
	{
		Dictionary<int, Color> remapColors;

		public static int GetRemapIndex(int[] ramp, int i)
		{
			return ramp[i];
		}

		public PlayerColorRemap(int[] ramp, HSLColor c, float rampFraction)
		{
			// Increase luminosity if required to represent the full ramp
			var rampRange = (byte)((1 - rampFraction) * c.L);
			var c1 = new HSLColor(c.H, c.S, Math.Max(rampRange, c.L)).RGB;
			var c2 = new HSLColor(c.H, c.S, (byte)Math.Max(0, c.L - rampRange)).RGB;
			var baseIndex = ramp[0];
			var remapRamp = ramp.Select(r => r - ramp[0]);
			var rampMaxIndex = ramp.Length - 1;

			// reversed remapping
			if (ramp[0] > ramp[rampMaxIndex])
			{
				baseIndex = ramp[rampMaxIndex];
				for (var i = rampMaxIndex; i > 0; i--)
					remapRamp = ramp.Select(r => r - ramp[rampMaxIndex]);
			}

			remapColors = remapRamp.Select((x, i) => Pair.New(baseIndex + i, Exts.ColorLerp(x / (float)ramp.Length, c1, c2)))
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
