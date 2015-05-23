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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Palette effect used for sprinkle \"animations\" on terrain tiles.")]
	class WaterPaletteRotationInfo : ITraitInfo
	{
		public readonly string[] ExcludePalettes = { };

		public object Create(ActorInitializer init) { return new WaterPaletteRotation(init.World, this); }
	}

	class WaterPaletteRotation : ITick, IPaletteModifier
	{
		readonly WaterPaletteRotationInfo info;
		readonly World world;
		float t = 0;

		public WaterPaletteRotation(World world, WaterPaletteRotationInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void Tick(Actor self) { t += .25f; }

		uint[] temp = new uint[7]; /* allocating this on the fly actually hurts our profile */

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			var rotate = (int)t % 7;
			if (rotate == 0)
				return;

			foreach (var kvp in palettes)
			{
				if (info.ExcludePalettes.Contains(kvp.Key))
					continue;

				var palette = kvp.Value;

				for (var i = 0; i < 7; i++)
					temp[(rotate + i) % 7] = palette[world.TileSet.WaterPaletteRotationBase + i];

				for (var i = 0; i < 7; i++)
					palette[world.TileSet.WaterPaletteRotationBase + i] = temp[i];
			}
		}
	}
}
