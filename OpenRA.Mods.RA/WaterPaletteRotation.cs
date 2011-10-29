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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class WaterPaletteRotationInfo : TraitInfo<WaterPaletteRotation> {}

	class WaterPaletteRotation : ITick, IPaletteModifier
	{
		float t = 0;
		public void Tick(Actor self)
		{
			t += .25f;
		}

		static string[] excludePalettes = { "cursor", "chrome", "colorpicker" };
		static uint[] temp = new uint[7];

		public void AdjustPalette(Dictionary<string,Palette> palettes)
		{
			foreach (var pal in palettes)
			{
				if (excludePalettes.Contains(pal.Key))
					continue;

				var colors = pal.Value.Values;
				var rotate = (int)t % 7;

				for (var i = 0; i < 7; i++)
					temp[(rotate + i) % 7] = colors[0x60 + i];

				for (var i = 0; i < 7; i++)
					pal.Value.SetColor(0x60 + i, temp[i]);
			}
		}
	}
}
