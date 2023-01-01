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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used for day/night effects.")]
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class GlobalLightingPaletteEffectInfo : TraitInfo, ILobbyCustomRulesIgnore
	{
		[Desc("Do not modify graphics that use any palette in this list.")]
		public readonly HashSet<string> ExcludePalettes = new HashSet<string> { "cursor", "chrome", "colorpicker", "fog", "shroud", "alpha" };

		[Desc("Do not modify graphics that start with these letters.")]
		public readonly HashSet<string> ExcludePalettePrefixes = new HashSet<string>();

		public readonly float Red = 1f;
		public readonly float Green = 1f;
		public readonly float Blue = 1f;
		public readonly float Ambient = 1f;

		public override object Create(ActorInitializer init) { return new GlobalLightingPaletteEffect(this); }
	}

	public class GlobalLightingPaletteEffect : IPaletteModifier
	{
		readonly GlobalLightingPaletteEffectInfo info;

		public float Red;
		public float Green;
		public float Blue;
		public float Ambient;

		public GlobalLightingPaletteEffect(GlobalLightingPaletteEffectInfo info)
		{
			this.info = info;

			Red = info.Red;
			Green = info.Green;
			Blue = info.Blue;
			Ambient = info.Ambient;
		}

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			// Calculate ambient color multipliers as integers for speed. To handle fractional ambiance, we'll increase
			// the magnitude of the result by 8 bits.
			var ar = (uint)((1 << 8) * Ambient * Red);
			var ag = (uint)((1 << 8) * Ambient * Green);
			var ab = (uint)((1 << 8) * Ambient * Blue);

			foreach (var kvp in palettes)
			{
				if (info.ExcludePalettes.Contains(kvp.Key))
					continue;

				if (info.ExcludePalettePrefixes.Any(kvp.Key.StartsWith))
					continue;

				var palette = kvp.Value;

				for (var x = 0; x < Palette.Size; x++)
				{
					/* Here is the reference code for the operation we are performing.
					var from = palette.GetColor(x);
					var r = (int)(from.R * Ambient * Red).Clamp(0, 255);
					var g = (int)(from.G * Ambient * Green).Clamp(0, 255);
					var b = (int)(from.B * Ambient * Blue).Clamp(0, 255);
					palette.SetColor(x, Color.FromArgb(from.A, r, g, b));
					*/

					// PERF: Use integer arithmetic to avoid costly conversions to and from floating point values.
					var from = palette[x];

					// 1: Extract each color component and shift it to the lower bits, then multiply with ambiance.
					// 2: Because the ambiance was increased by 8 bits, our result has been shifted 8 bits up.
					// If the multiply overflowed we clamp the value, otherwise we mask out the fractional bits.
					// 3: Finally, we shift the color component back to its correct place. We're already 8 bits higher
					// than expected due to the multiply, so we don't have to shift as far to get back.
					var r1 = ((from & 0x00FF0000) >> 16) * ar;
					var r2 = r1 >= 0x0000FF00 ? 0x0000FF00 : r1 & 0x0000FF00;
					var r3 = r2 << 8;

					var g1 = ((from & 0x0000FF00) >> 8) * ag;
					var g2 = g1 >= 0x0000FF00 ? 0x0000FF00 : g1 & 0x0000FF00;
					var g3 = g2 << 0;

					var b1 = ((from & 0x000000FF) >> 0) * ab;
					var b2 = b1 >= 0x0000FF00 ? 0x0000FF00 : b1 & 0x0000FF00;
					var b3 = b2 >> 8;

					// Combine all the adjusted components back together.
					var a = from & 0xFF000000;
					palette[x] = a | r3 | g3 | b3;
				}
			}
		}
	}
}
