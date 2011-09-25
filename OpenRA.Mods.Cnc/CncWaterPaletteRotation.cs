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
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class CncWaterPaletteRotationInfo : TraitInfo<CncWaterPaletteRotation> {}

	class CncWaterPaletteRotation : ITick, IPaletteModifier
	{
		float t = 0;

		public void Tick(Actor self) { t += .25f; }

		public void AdjustPalette(Dictionary<string,Palette> palettes)
		{
			// Only modify the terrain palette
			var pal = palettes["terrain"];

			var copy = (uint[])pal.Values.Clone();
			var rotate = (int)t % 7;

			for (int i = 0; i < 7; i++)
				pal.SetColor(0x20 + (rotate + i) % 7, copy[0x20 + i]);
		}
	}
}
