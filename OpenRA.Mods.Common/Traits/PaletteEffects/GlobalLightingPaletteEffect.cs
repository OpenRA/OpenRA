#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used for day/night effects.")]
	class GlobalLightingPaletteEffectInfo : ITraitInfo
	{
		[Desc("Do not modify graphics that use any palette in this list.")]
		public readonly HashSet<string> ExcludePalettes = new HashSet<string> { "cursor", "chrome", "colorpicker", "fog", "shroud", "alpha" };

		[Desc("Do not modify graphics that start with these letters.")]
		public readonly HashSet<string> ExcludePalettePrefixes = new HashSet<string>();

		public readonly float Red = 1f;
		public readonly float Green = 1f;
		public readonly float Blue = 1f;
		public readonly float Ambient = 1f;

		public object Create(ActorInitializer init) { return new GlobalLightingPaletteEffect(this); }
	}

	class GlobalLightingPaletteEffect : IPaletteModifier
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
			foreach (var kvp in palettes)
			{
				if (info.ExcludePalettes.Contains(kvp.Key))
					continue;

				if (info.ExcludePalettePrefixes.Any(kvp.Key.StartsWith))
					continue;

				var palette = kvp.Value;

				for (var x = 0; x < Palette.Size; x++)
				{
					var from = palette.GetColor(x);
					var red = (int)(from.R * Ambient * Red).Clamp(0, 255);
					var green = (int)(from.G * Ambient * Green).Clamp(0, 255);
					var blue = (int)(from.B * Ambient * Blue).Clamp(0, 255);
					palette.SetColor(x, Color.FromArgb(from.A, red, green, blue));
				}
			}
		}
	}
}
