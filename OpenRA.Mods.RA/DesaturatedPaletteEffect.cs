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
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class DesaturatedPaletteEffectInfo : TraitInfo<DesaturatedPaletteEffect> { }

	public class DesaturatedPaletteEffect : IPaletteModifier
	{
		// Doing this every frame is stupid
		public void AdjustPalette(Dictionary<string,Palette> palettes)
		{
			var excludePalettes = new List<string>(){"cursor", "chrome", "colorpicker", "shroud", "fog"};
			foreach (var pal in palettes)
			{
				if (excludePalettes.Contains(pal.Key))
					continue;
				
				for (var x = 0; x < 256; x++)
				{
					var orig = pal.Value.GetColor(x);
					var lum = (int)(255 * orig.GetBrightness());
					pal.Value.SetColor(x, Color.FromArgb(orig.A, lum, lum, lum));
				}
			}
		}
	}
}
