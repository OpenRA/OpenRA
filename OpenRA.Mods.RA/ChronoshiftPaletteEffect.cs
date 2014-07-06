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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ChronoshiftPaletteEffectInfo : TraitInfo<ChronoshiftPaletteEffect> { }

	public class ChronoshiftPaletteEffect : IPaletteModifier, ITickRender
	{
		const int chronoEffectLength = 60;
		int remainingFrames;

		public void Enable()
		{
			remainingFrames = chronoEffectLength;
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (wr.world.Paused == World.PauseState.Paused)
				return;

			if (remainingFrames > 0)
				remainingFrames--;
		}

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			if (remainingFrames == 0)
				return;

			var frac = (float)remainingFrames / chronoEffectLength;

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
