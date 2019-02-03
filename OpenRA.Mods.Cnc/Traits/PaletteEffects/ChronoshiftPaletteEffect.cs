#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Apply palette full screen rotations during chronoshifts. Add this to the world actor.")]
	public class ChronoshiftPaletteEffectInfo : ITraitInfo
	{
		[Desc("Measured in ticks.")]
		public readonly int ChronoEffectLength = 60;

		public object Create(ActorInitializer init) { return new ChronoshiftPaletteEffect(this); }
	}

	public class ChronoshiftPaletteEffect : IPaletteModifier, ITick
	{
		readonly ChronoshiftPaletteEffectInfo info;
		int remainingFrames;

		public ChronoshiftPaletteEffect(ChronoshiftPaletteEffectInfo info)
		{
			this.info = info;
		}

		public void Enable()
		{
			remainingFrames = info.ChronoEffectLength;
		}

		void ITick.Tick(Actor self)
		{
			if (remainingFrames > 0)
				remainingFrames--;
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			if (remainingFrames == 0)
				return;

			var frac = (float)remainingFrames / info.ChronoEffectLength;

			foreach (var pal in palettes)
			{
				for (var x = 0; x < Palette.Size; x++)
				{
					var orig = pal.Value.GetColor(x);
					var lum = (int)(255 * orig.GetBrightness());
					var desat = Color.FromArgb(orig.A, lum, lum, lum);
					pal.Value.SetColor(x, Exts.ColorLerp(frac, orig, desat));
				}
			}
		}
	}
}
