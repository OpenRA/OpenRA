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

namespace OpenRA.Mods.Cnc
{
	class CncMenuPaletteEffectInfo : TraitInfo<CncMenuPaletteEffect> { }

	public class CncMenuPaletteEffect : IPaletteModifier, ITick
	{		
		public enum EffectType { None, Black, Desaturated }

		const int effectLength = 10;
		int remainingFrames;
		
		EffectType from = EffectType.Black;
		EffectType to = EffectType.Black;
		
		public void Fade(EffectType type)
		{
			remainingFrames = effectLength;
			from = to;
			to = type;
		}

		public void Tick(Actor self)
		{
			if (remainingFrames > 0)
				remainingFrames--;
		}
		
		Color ColorForEffect(EffectType t, Color orig)
		{
			switch(t)
			{
				case EffectType.Black:
					return Color.FromArgb(orig.A, Color.Black);
				case EffectType.Desaturated:
					var lum = (int)(255 * orig.GetBrightness());
					return Color.FromArgb(orig.A, lum, lum, lum);
				default:
				case EffectType.None:
					return orig;
			}
		}
		
		public void AdjustPalette(Dictionary<string,Palette> palettes)
		{
			if (to == EffectType.None && remainingFrames == 0)
				return;
			
			var excludePalettes = new List<string>(){"cursor", "chrome", "colorpicker", "shroud", "fog"};
			foreach (var pal in palettes)
			{
				if (excludePalettes.Contains(pal.Key))
					continue;
				
				for (var x = 0; x < 256; x++)
				{
					var orig = pal.Value.GetColor(x);
					var t = ColorForEffect(to, orig);
					
					if (remainingFrames == 0)
						pal.Value.SetColor(x, t);
					else
					{
						var f = ColorForEffect(from, orig);
						pal.Value.SetColor(x, OpenRA.Graphics.Util.Lerp((float)remainingFrames / effectLength, t, f));
					}
				}
			}
		}
	}
}
