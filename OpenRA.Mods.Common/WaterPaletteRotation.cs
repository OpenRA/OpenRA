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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("Palette effect used for sprinkle \"animations\" on terrain tiles.")]
	class WaterPaletteRotationInfo : ITraitInfo
	{
		public readonly string[] ExcludePalettes = {};

		public object Create(ActorInitializer init) { return new WaterPaletteRotation(init.world, this); }
	}

	class WaterPaletteRotation : ITick, IPaletteModifier
	{
		float t = 0;

		readonly WaterPaletteRotationInfo info;
		readonly World world;

		public WaterPaletteRotation(World world, WaterPaletteRotationInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void Tick(Actor self) { t += .25f; }

		static uint[] temp = new uint[7]; /* allocating this on the fly actually hurts our profile */

		public void AdjustPalette(Dictionary<string,Palette> palettes)
		{
			foreach (var pal in palettes)
			{
				if (info.ExcludePalettes.Contains(pal.Key))
					continue;

				var colors = pal.Value.Values;
				var rotate = (int)t % 7;

				for (var i = 0; i < 7; i++)
					temp[(rotate + i) % 7] = colors[world.TileSet.WaterPaletteRotationBase + i];

				for (var i = 0; i < 7; i++)
					pal.Value.SetColor(world.TileSet.WaterPaletteRotationBase + i, temp[i]);
			}
		}
	}
}
