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

namespace OpenRA.Mods.RA
{
	class WaterPaletteRotationInfo : TraitInfo<WaterPaletteRotation> { }

	class WaterPaletteRotation : ITick, IPaletteModifier
	{
		float t = 0;
		public void Tick(Actor self)
		{
			t += .25f;
		}

		public void AdjustPalette(Bitmap b)
		{
			var rotate = (int)t % 7;
			using (var bitmapCopy = new Bitmap(b))
				for (int j = 0; j < b.Height; j++)
					for (int i = 0; i < 7; i++)
						b.SetPixel(0x60 + (rotate + i) % 7, j, bitmapCopy.GetPixel(0x60 + i, j));
		}
	}
}
