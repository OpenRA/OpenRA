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
	public class CreateEffectWarhead : Warhead
	{
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

		public override void DoImpact(Target target, Actor firedBy, float firepowerModifier)
		{
			DoImpact(target.CenterPosition, firedBy, firepowerModifier);
		}

		public void DoImpact(WPos pos, Actor firedBy, float firepowerModifier)
		{
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);

			if (!world.Map.Contains(targetTile))
				return;

			// TODO: #5937 should go in here after rebase.
			var isWater = pos.Z <= 0 && world.Map.GetTerrainInfo(targetTile).IsWater;
			var explosionType = isWater ? WaterExplosion : Explosion;
			var explosionTypePalette = isWater ? WaterExplosionPalette : ExplosionPalette;

			if (explosionType != null)
				world.AddFrameEndTask(w => w.Add(new Explosion(w, pos, explosionType, explosionTypePalette)));

			var sound = ImpactSound;

			var isTargetWater = pos.Z <= 0 && world.Map.GetTerrainInfo(targetTile).IsWater;
			if (isTargetWater && WaterImpactSound != null)
				sound = WaterImpactSound;

			Sound.Play(sound, pos);
		}

		public override float EffectivenessAgainst(ActorInfo ai) { return 1f; }
	}
}
