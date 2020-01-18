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

		public PlayerColorRemap(int[] ramp, Color c, float rampFraction)
		{
			var h = c.GetHue() / 360.0f;
			var s = c.GetSaturation();
			var l = c.GetBrightness();

			// Increase luminosity if required to represent the full ramp
			var rampRange = (byte)((1 - rampFraction) * l);
			var c1 = Color.FromAhsl(h, s, Math.Max(rampRange, l));
			var c2 = Color.FromAhsl(h, s, (byte)Math.Max(0, l - rampRange));
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
