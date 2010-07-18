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
	class ChronoshiftPaletteEffectInfo : TraitInfo<ChronoshiftPaletteEffect> { }

	public class ChronoshiftPaletteEffect : IPaletteModifier, ITick
	{
		const int chronoEffectLength = 20;
		int remainingFrames;

		public void Enable()
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
			
			var frac = (float)remainingFrames / chronoEffectLength;

			// TODO: Fix me to only affect "world" palettes
			for( var y = 0; y < b.Height; y++ )
				for (var x = 0; x < 256; x++)
				{
					var orig = b.GetPixel(x, y);
					var lum = (int)(255 * orig.GetBrightness());
					var desat = Color.FromArgb(orig.A, lum, lum, lum);
					b.SetPixel(x, y, OpenRA.Graphics.Util.Lerp(frac, orig, desat));
				}
		}
	}
}
