#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
	public class CloakPaletteEffectInfo : TraitInfo<CloakPaletteEffect> { }

	public class CloakPaletteEffect : IPaletteModifier, ITickRender
	{
		float t = 0;
		string paletteName = "cloak";

		Color[] colors = {
			Color.FromArgb(55, 205, 205, 220),
			Color.FromArgb(120, 205, 205, 230),
			Color.FromArgb(192, 180, 180, 255),
			Color.FromArgb(178, 205, 250, 220),
		};

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
		{
			var i = (int)t;
			var p = b[paletteName];

			for (var j = 0; j < colors.Length; j++)
			{
				var k = (i + j) % 16 + 0xb0;
				p.SetColor(k, colors[j]);
			}
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (wr.world.Paused == World.PauseState.Paused)
				return;

			t += 0.25f;
			if (t >= 256) t = 0;
		}
	}
}
