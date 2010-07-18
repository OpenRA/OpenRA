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
	class LightPaletteRotatorInfo : TraitInfo<LightPaletteRotator> { }
	class LightPaletteRotator : ITick, IPaletteModifier
	{
		float t = 0;
		public void Tick(Actor self)
		{
			t += .5f;
		}

		public void AdjustPalette(Bitmap b)
		{
			var rotate = (int)t % 18;
			if (rotate > 9)
				rotate = 18 - rotate;
			
			using (var bitmapCopy = new Bitmap(b))
				for (int j = 0; j < b.Height; j++)
					b.SetPixel(0x67, j, b.GetPixel(230+rotate, j));
		}
	}
}
