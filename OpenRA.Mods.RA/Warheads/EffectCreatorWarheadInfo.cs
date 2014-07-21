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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Mods.RA.Effects;

namespace OpenRA.Mods.RA
{
	public class EffectCreatorWarheadInfo : BaseWarhead, IWarheadInfo
	{
		[Desc("Size of the area. An explosion animation will be created in each tile.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		[Desc("Explosion effect to use.")]
		public readonly string Explosion = null;

		[Desc("Palette to use for explosion effect.")]
		public readonly string ExplosionPalette = "effect";

		[Desc("Explosion effect on hitting water (usually a splash).")]
		public readonly string WaterExplosion = null;

		[Desc("Palette to use for effect on hitting water (usually a splash).")]
		public readonly string WaterExplosionPalette = "effect";

		[Desc("Sound to play on impact.")]
		public readonly string ImpactSound = null;

		[Desc("Sound to play on impact with water")]
		public readonly string WaterImpactSound = null;

		public EffectCreatorWarheadInfo() : base() { }

		public new void DoImpact(WPos pos, WeaponInfo weapon, Actor firedBy, float firepowerModifier)
		{
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);

			if (!world.Map.Contains(targetTile))
				return;

			if (Size[0] > 0)
			{
				var allCells = world.Map.FindTilesInCircle(targetTile, Size[0]).ToList();
				// `deltaCells` might want to just be an outer shell of the cells:
				IEnumerable<CPos> deltaCells = allCells;
				if (Size.Length == 2)
					deltaCells = deltaCells.Except(world.Map.FindTilesInCircle(targetTile, Size[1]));

				// Draw the effects
				foreach (var currentCell in deltaCells)
				{
					var currentPos = world.Map.CenterOfCell(currentCell);
					var isWater = currentPos.Z <= 0 && world.Map.GetTerrainInfo(currentCell).IsWater;
					var explosionType = isWater ? WaterExplosion : Explosion;
					var explosionTypePalette = isWater ? WaterExplosionPalette : ExplosionPalette;

					if (explosionType != null)
						world.AddFrameEndTask(w => w.Add(new Explosion(w, currentPos, explosionType, explosionTypePalette)));
				}
			}
			else
			{
				var isWater = pos.Z <= 0 && world.Map.GetTerrainInfo(targetTile).IsWater;
				var explosionType = isWater ? WaterExplosion : Explosion;
				var explosionTypePalette = isWater ? WaterExplosionPalette : ExplosionPalette;

				if (explosionType != null)
					world.AddFrameEndTask(w => w.Add(new Explosion(w, pos, explosionType, explosionTypePalette)));
			}

			string sound = null;

			var isTargetWater = pos.Z <= 0 && world.Map.GetTerrainInfo(targetTile).IsWater;
			if (isTargetWater && WaterImpactSound != null)
				sound = WaterImpactSound;

			if (ImpactSound != null)
				sound = ImpactSound;

			Sound.Play(sound, pos);
		}
	}
}
