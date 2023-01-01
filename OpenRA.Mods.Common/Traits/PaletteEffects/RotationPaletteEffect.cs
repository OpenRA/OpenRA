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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Palette effect used for sprinkle \"animations\".")]
	class RotationPaletteEffectInfo : TraitInfo
	{
		[Desc("Defines to which palettes this effect should be applied to.",
			"If none specified, it applies to all palettes not explicitly excluded.")]
		public readonly HashSet<string> Palettes = new HashSet<string>();

		[Desc("Defines for which tileset IDs this effect should be loaded.",
			"If none specified, it applies to all tileset IDs not explicitly excluded.")]
		public readonly HashSet<string> Tilesets = new HashSet<string>();

		[Desc("Defines which palettes should be excluded from this effect.")]
		public readonly HashSet<string> ExcludePalettes = new HashSet<string>();

		[Desc("Don't apply the effect for these tileset IDs.")]
		public readonly HashSet<string> ExcludeTilesets = new HashSet<string>();

		[Desc("Palette index of first RotationRange color.")]
		public readonly int RotationBase = 0x60;

		[Desc("Range of colors to rotate.")]
		public readonly int RotationRange = 7;

		[Desc("Step towards next color index per tick.")]
		public readonly float RotationStep = .25f;

		public override object Create(ActorInitializer init) { return new RotationPaletteEffect(init.World, this); }
	}

	class RotationPaletteEffect : ITick, IPaletteModifier
	{
		readonly RotationPaletteEffectInfo info;
		readonly uint[] rotationBuffer;
		readonly bool validTileset;
		readonly string tilesetId;
		float t = 0;

		public RotationPaletteEffect(World world, RotationPaletteEffectInfo info)
		{
			this.info = info;
			rotationBuffer = new uint[info.RotationRange];
			tilesetId = world.Map.Rules.TerrainInfo.Id;

			validTileset = IsValidTileset();
		}

		bool IsValidTileset()
		{
			if (info.Tilesets.Count == 0 && info.ExcludeTilesets.Count == 0)
				return true;

			if (info.Tilesets.Count == 0 && !info.ExcludeTilesets.Contains(tilesetId))
				return true;

			return info.Tilesets.Contains(tilesetId) && !info.ExcludeTilesets.Contains(tilesetId);
		}

		void ITick.Tick(Actor self)
		{
			if (!validTileset)
				return;

			t += info.RotationStep;
		}

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			if (!validTileset)
				return;

			var rotate = (int)t % info.RotationRange;
			if (rotate == 0)
				return;

			foreach (var kvp in palettes)
			{
				if ((info.Palettes.Count > 0 && !StartsWithAny(kvp.Key, info.Palettes))
					|| (info.ExcludePalettes.Count > 0 && StartsWithAny(kvp.Key, info.ExcludePalettes)))
					continue;

				var palette = kvp.Value;

				for (var i = 0; i < info.RotationRange; i++)
					rotationBuffer[(rotate + i) % info.RotationRange] = palette[info.RotationBase + i];

				for (var i = 0; i < info.RotationRange; i++)
					palette[info.RotationBase + i] = rotationBuffer[i];
			}
		}

		static bool StartsWithAny(string name, HashSet<string> prefixes)
		{
			// PERF: Avoid LINQ.
			foreach (var pref in prefixes)
				if (name.StartsWith(pref))
					return true;

			return false;
		}
	}
}
