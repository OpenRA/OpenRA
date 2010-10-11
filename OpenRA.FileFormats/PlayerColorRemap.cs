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
	// TODO: ship this out of here.
	public enum PaletteFormat { ra, cnc, d2k }

	public class PlayerColorRemap : IPaletteRemap
	{
		Dictionary<int, Color> remapColors;

		public PlayerColorRemap(Color c1, Color c2, PaletteFormat fmt)
		{
			var baseIndex = (fmt == PaletteFormat.cnc) ? 0xb0 : (fmt == PaletteFormat.d2k) ? 240 : 80;
			var ramp = (fmt == PaletteFormat.cnc)
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

		// hk is hue in the range [0,1] instead of [0,360]
		public static Color ColorFromHSL(float hk, float s, float l)
		{
			// Convert from HSL to RGB
			var q = (l < 0.5f) ? l * (1 + s) : l + s - (l * s);
			var p = 2 * l - q;

			float[] trgb = { hk + 1 / 3.0f,
							  hk,
							  hk - 1/3.0f };
			float[] rgb = { 0, 0, 0 };

			for (int k = 0; k < 3; k++)
			{
				while (trgb[k] < 0) trgb[k] += 1.0f;
				while (trgb[k] > 1) trgb[k] -= 1.0f;
			}

			for (int k = 0; k < 3; k++)
			{
				if (trgb[k] < 1 / 6.0f) { rgb[k] = (p + ((q - p) * 6 * trgb[k])); }
				else if (trgb[k] >= 1 / 6.0f && trgb[k] < 0.5) { rgb[k] = q; }
				else if (trgb[k] >= 0.5f && trgb[k] < 2.0f / 3) { rgb[k] = (p + ((q - p) * 6 * (2.0f / 3 - trgb[k]))); }
				else { rgb[k] = p; }
			}

			return Color.FromArgb((int)(rgb[0] * 255), (int)(rgb[1] * 255), (int)(rgb[2] * 255));
		}
	}
}
