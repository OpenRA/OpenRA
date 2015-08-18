#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
	[Desc("Palette effect used for sprinkle \"animations\".")]
	class WaterPaletteRotationInfo : ITraitInfo
	{
		[Desc("Defines which palettes should be excluded from this effect.")]
		public readonly HashSet<string> ExcludePalettes = new HashSet<string>();

		[Desc("Don't apply the effect for these tileset IDs.")]
		public readonly string[] ExcludeTilesets = { };

		[Desc("Palette index of first RotationRange color.")]
		public readonly int RotationBase = 0x60;

		[Desc("Range of colors to rotate.")]
		public readonly int RotationRange = 7;

		[Desc("Step towards next color index per tick.")]
		public readonly float RotationStep = .25f;

		public object Create(ActorInitializer init) { return new WaterPaletteRotation(init.World, this); }
	}

	class WaterPaletteRotation : ITick, IPaletteModifier
	{
		readonly WaterPaletteRotationInfo info;
		readonly World world;
		float t = 0;
		uint[] temp;

		public WaterPaletteRotation(World world, WaterPaletteRotationInfo info)
		{
			this.world = world;
			this.info = info;

			temp = new uint[info.RotationRange]; /* allocating this on the fly actually hurts our profile */
		}

		public void Tick(Actor self)
		{
			t += info.RotationStep;
		}

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			var rotate = (int)t % info.RotationRange;
			if (rotate == 0)
				return;

			foreach (var kvp in palettes)
			{
				if (info.ExcludePalettes.Contains(kvp.Key) || info.ExcludeTilesets.Contains(world.TileSet.Id))
					continue;

				var palette = kvp.Value;

				for (var i = 0; i < info.RotationRange; i++)
					temp[(rotate + i) % info.RotationRange] = palette[info.RotationBase + i];

				for (var i = 0; i < info.RotationRange; i++)
					palette[info.RotationBase + i] = temp[i];
			}
		}
	}
}
