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

using System;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class PlayerColorRemap : IPaletteRemap
	{
		readonly int[] remapIndices;
		readonly float hue;
		readonly float saturation;

		public PlayerColorRemap(int[] remapIndices, float hue, float saturation)
		{
			this.remapIndices = remapIndices;
			this.hue = hue;
			this.saturation = saturation;
		}

		public Color GetRemappedColor(Color original, int index)
		{
			if (!remapIndices.Contains(index))
				return original;

			// Color remapping is applied in a linear color space, so start
			// by undoing the pre-multiplied alpha and gamma corrections
			var (r, g, b) = original.ToLinear();

			// Calculate the brightness (i.e HSV value) of the original colour
			// This inlines the single line of Color.RgbToHsv() that we need
			var value = Math.Max(Math.Max(r, g), b);

			// Construct the new RGB color
			(r, g, b) = Color.HsvToRgb(hue, saturation, value);

			// Convert linear back to SRGB and pre-multiply by the alpha
			return Color.FromLinear(original.A, r, g, b);
		}
	}
}
