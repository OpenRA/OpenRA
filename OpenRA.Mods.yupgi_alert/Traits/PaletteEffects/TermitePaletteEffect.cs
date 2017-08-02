#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

/* Works without base engine modification */

namespace OpenRA.Mods.Common.Traits
{
	public class TermitePaletteEffectInfo : ITraitInfo
	{
		[Desc("The palette to apply this effect to.")]
		public readonly string PaletteName = "termite";

		[Desc("Period of random palette generation")]
		public readonly int Period = 16;

		public object Create(ActorInitializer init) { return new TermitePaletteEffect(this); }
	}

	public class TermitePaletteEffect : IPaletteModifier, ITick
	{
		TermitePaletteEffectInfo info;
		int ticks = 0;
		int paletteShift = 0;

		public TermitePaletteEffect(TermitePaletteEffectInfo info)
		{
			this.info = info;
		}

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
		{
			var p = b[info.PaletteName];

			// modify all colors except index 0 which is the transparent color.
			for (int j = 1; j < Palette.Size; j++)
			{
				switch ((j + paletteShift) % 8)
				{
					case 0:
						p.SetColor(j, Color.FromArgb(64, Color.Brown));
						break;
					case 1:
						p.SetColor(j, Color.FromArgb(64, Color.Gold));
						break;
					default:
						break;
				}
			}
		}

		public void Tick(Actor self)
		{
			ticks--;
			if (ticks > 0)
				return;

			ticks = info.Period;
			paletteShift = (paletteShift + 1) % 8;
		}
	}
}
