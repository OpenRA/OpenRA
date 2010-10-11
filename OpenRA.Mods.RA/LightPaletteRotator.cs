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
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class LightPaletteRotatorInfo : TraitInfo<LightPaletteRotator> { }
	class LightPaletteRotator : ITick, IPaletteModifier
	{
		float t = 0;
		public void Tick(Actor self)
		{
			t += .5f;
		}
		
		public void AdjustPalette(Dictionary<string,Palette> palettes)
		{
			var excludePalettes = new List<string>(){"cursor", "chrome", "colorpicker"};
			foreach (var pal in palettes)
			{
				if (excludePalettes.Contains(pal.Key))
					continue;
				
				var rotate = (int)t % 18;
				if (rotate > 9)
					rotate = 18 - rotate;
				
				pal.Value.SetColor(0x67, pal.Value.GetColor(230+rotate));
			}
		}
	}
}
