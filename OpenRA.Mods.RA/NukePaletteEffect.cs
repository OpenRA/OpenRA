#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Traits;
using System.Collections.Generic;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
	class NukePaletteEffectInfo : TraitInfo<NukePaletteEffect> { }

	public class NukePaletteEffect : IPaletteModifier, ITick
	{
		const int nukeEffectLength = 20;
		int remainingFrames;

		public void Enable()
		{
			remainingFrames = nukeEffectLength;
		}

		public void Tick(Actor self)
		{
			if (remainingFrames > 0)
				remainingFrames--;
		}

		public void AdjustPalette(Dictionary<string,Palette> palettes)
		{
			if (remainingFrames == 0)
				return;
			
			var frac = (float)remainingFrames / nukeEffectLength;
			
			var excludePalettes = new List<string>(){"cursor", "chrome", "colorpicker"};
			foreach (var pal in palettes)
			{
				if (excludePalettes.Contains(pal.Key))
					continue;
				
				for (var x = 0; x < 256; x++)
				{
					var orig = pal.Value.GetColor(x);
					var white = Color.FromArgb(orig.A, 255, 255, 255);
					pal.Value.SetColor(x, OpenRA.Graphics.Util.Lerp(frac,orig,white));
				}
			}
		}
	}
}
