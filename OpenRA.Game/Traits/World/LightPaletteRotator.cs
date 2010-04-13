#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Drawing;

namespace OpenRA.Traits
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
				for (int j = 0; j < 16; j++)
					b.SetPixel(0x67, j, b.GetPixel(230+rotate, j));
		}
	}
}
