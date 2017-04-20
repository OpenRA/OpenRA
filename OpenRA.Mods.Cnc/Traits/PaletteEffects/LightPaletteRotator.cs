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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Palette effect used for blinking \"animations\" on actors.")]
	class LightPaletteRotatorInfo : ITraitInfo
	{
		[Desc("Palettes this effect should not apply to.")]
		public readonly HashSet<string> ExcludePalettes = new HashSet<string>();

		public object Create(ActorInitializer init) { return new LightPaletteRotator(this); }
	}

	class LightPaletteRotator : ITick, IPaletteModifier
	{
		readonly LightPaletteRotatorInfo info;
		float t = 0;

		public LightPaletteRotator(LightPaletteRotatorInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			t += .5f;
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			foreach (var pal in palettes)
			{
				if (info.ExcludePalettes.Contains(pal.Key))
					continue;

				var rotate = (int)t % 18;
				if (rotate > 9)
					rotate = 18 - rotate;

				pal.Value.SetColor(0x67, pal.Value.GetColor(230 + rotate));
			}
		}
	}
}
