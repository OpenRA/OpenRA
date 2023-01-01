#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class CloakPaletteEffectInfo : TraitInfo<CloakPaletteEffect> { }

	public class CloakPaletteEffect : IPaletteModifier, ITick
	{
		float t = 0;
		readonly string paletteName = "cloak";

		readonly Color[] colors =
		{
			Color.FromArgb(55, 205, 205, 220),
			Color.FromArgb(120, 205, 205, 230),
			Color.FromArgb(192, 180, 180, 255),
			Color.FromArgb(178, 205, 250, 220),
		};

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
		{
			var i = (int)t;
			var p = b[paletteName];

			for (var j = 0; j < colors.Length; j++)
			{
				var k = (i + j) % 16 + 0xb0;
				p.SetColor(k, colors[j]);
			}
		}

		void ITick.Tick(Actor self)
		{
			t += 0.25f;
			if (t >= 256) t = 0;
		}
	}
}
