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
	class RotationPaletteEffectInfo : ITraitInfo
	{
		[Desc("Defines to which palettes this effect should be applied to.",
			"If none specified, it applies to all palettes not explicitly excluded.")]
		public readonly HashSet<string> Palettes = new HashSet<string>();

		[Desc("Defines for which tileset IDs this effect should be loaded.",
			"If none specified, it applies to all tileset IDs not explicitly excluded.")]
		public readonly string[] Tilesets = null;

		[Desc("Defines which palettes should be excluded from this effect.")]
		public readonly HashSet<string> ExcludePalettes = new HashSet<string>();

		[Desc("Don't apply the effect for these tileset IDs.")]
		public readonly string[] ExcludeTilesets = null;

		[Desc("Palette index of first RotationRange color.")]
		public readonly int RotationBase = 0x60;

		[Desc("Range of colors to rotate.")]
		public readonly int RotationRange = 7;

		[Desc("Step towards next color index per tick.")]
		public readonly float RotationStep = .25f;

		public object Create(ActorInitializer init) { return new RotationPaletteEffect(init.World, this); }
	}

	class RotationPaletteEffect : ITick, IPaletteModifier
	{
		readonly RotationPaletteEffectInfo info;
		readonly World world;
		readonly uint[] rotationBuffer;
		float t = 0;

		public RotationPaletteEffect(World world, RotationPaletteEffectInfo info)
		{
			this.world = world;
			this.info = info;

			rotationBuffer = new uint[info.RotationRange];
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
				if ((info.Palettes.Count > 0 && !info.Palettes.Any(kvp.Key.StartsWith))
					|| (info.Tilesets != null && !info.Tilesets.Contains(world.TileSet.Id))
					|| (info.ExcludePalettes.Count > 0 && info.ExcludePalettes.Any(kvp.Key.StartsWith))
					|| (info.ExcludeTilesets != null && info.ExcludeTilesets.Contains(world.TileSet.Id)))
					continue;

				var palette = kvp.Value;

				for (var i = 0; i < info.RotationRange; i++)
					rotationBuffer[(rotate + i) % info.RotationRange] = palette[info.RotationBase + i];

				for (var i = 0; i < info.RotationRange; i++)
					palette[info.RotationBase + i] = rotationBuffer[i];
			}
		}
	}
}
