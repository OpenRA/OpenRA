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

namespace OpenRa.Traits
{
	class ChronoshiftPaletteEffectInfo : StatelessTraitInfo<ChronoshiftPaletteEffect> { }

	public class ChronoshiftPaletteEffect : IPaletteModifier, ITick
	{
		const int chronoEffectLength = 20;
		int remainingFrames;

		public void DoChronoshift()
		{
			remainingFrames = chronoEffectLength;
		}

		public void Tick(Actor self)
		{
			if (remainingFrames > 0)
				remainingFrames--;
		}

		public void AdjustPalette(Bitmap b)
		{
			if (remainingFrames == 0)
				return;
			/* TODO: FIX ME
			var frac = (float)remainingFrames / chronoEffectLength;
			for( var y = 0; y < (int)PaletteType.Chrome; y++ )
				for (var x = 0; x < 256; x++)
				{
					var orig = b.GetPixel(x, y);
					var lum = (int)(255 * orig.GetBrightness());
					var desat = Color.FromArgb(orig.A, lum, lum, lum);
					b.SetPixel(x, y, Graphics.Util.Lerp(frac, orig, desat));
				}
			*/
		}
	}
}
