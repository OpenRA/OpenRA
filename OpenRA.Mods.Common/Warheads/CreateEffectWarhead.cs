#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class CreateEffectWarhead : Warhead
	{
		[Desc("List of explosion sequences that can be used.")]
		[SequenceReference("Image")] public readonly string[] Explosions = new string[0];

		[Desc("Image containing explosion effect sequence.")]
		public readonly string Image = "explosion";

		[Desc("Palette to use for explosion effect."), PaletteReference("UsePlayerPalette")]
		public readonly string ExplosionPalette = "effect";

		[Desc("Remap explosion effect to player color, if art supports it.")]
		public readonly bool UsePlayerPalette = false;

		[Desc("Search radius around impact for 'direct hit' check.")]
		public readonly WDist TargetSearchRadius = new WDist(2048);

		[Desc("List of sounds that can be played on impact.")]
		public readonly string[] ImpactSounds = new string[0];

		[Desc("What impact types should this effect apply to.")]
		public readonly ImpactType ValidImpactTypes = ImpactType.Ground | ImpactType.Water | ImpactType.Air | ImpactType.GroundHit | ImpactType.WaterHit | ImpactType.AirHit;

		[Desc("What impact types should this effect NOT apply to.", "Overrides ValidImpactTypes.")]
		public readonly ImpactType InvalidImpactTypes = ImpactType.None;

		public ImpactType GetImpactType(World world, CPos cell, WPos pos, Actor firedBy)
		{
			// Missiles need a margin because they sometimes explode a little above ground
			// due to their explosion check triggering slightly too early (because of CloseEnough).
			// TODO: Base ImpactType on target altitude instead of explosion altitude.
			var airMargin = new WDist(128);

			// Matching target actor
			if (ValidImpactTypes.HasFlag(ImpactType.TargetHit) && GetDirectHit(world, cell, pos, firedBy, true))
				return ImpactType.TargetHit;

			var dat = world.Map.DistanceAboveTerrain(pos);
			var isDirectHit = GetDirectHit(world, cell, pos, firedBy);

			if (dat.Length > airMargin.Length)
				return isDirectHit ? ImpactType.AirHit : ImpactType.Air;

			if (dat.Length <= airMargin.Length && world.Map.GetTerrainInfo(cell).IsWater)
				return isDirectHit ? ImpactType.WaterHit : ImpactType.Water;

			if (isDirectHit)
				return ImpactType.GroundHit;

			// Matching target terrain
			if (ValidImpactTypes.HasFlag(ImpactType.TargetTerrain)
				&& IsValidTarget(world.Map.GetTerrainInfo(cell).TargetTypes))
				return ImpactType.TargetTerrain;

			return ImpactType.Ground;
		}

		public bool GetDirectHit(World world, CPos cell, WPos pos, Actor firedBy, bool checkTargetType = false)
		{
			foreach (var victim in world.FindActorsInCircle(pos, TargetSearchRadius))
			{
				if (checkTargetType && !IsValidAgainst(victim, firedBy))
					continue;

				var healthInfo = victim.Info.TraitInfoOrDefault<HealthInfo>();
				if (healthInfo == null)
					continue;

				// If the impact position is within any actor's HitShape, we have a direct hit
				if (healthInfo.Shape.DistanceFromEdge(pos, victim).Length <= 0)
					return true;
			}

			return false;
		}

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!target.IsValidFor(firedBy))
				return;

			var pos = target.CenterPosition;
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);
			var isValid = IsValidImpact(pos, firedBy);

			if ((!world.Map.Contains(targetTile)) || (!isValid))
				return;

			var palette = ExplosionPalette;
			if (UsePlayerPalette)
				palette += firedBy.Owner.InternalName;

			var explosion = Explosions.RandomOrDefault(Game.CosmeticRandom);
			if (Image != null && explosion != null)
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, w, Image, explosion, palette)));

			var impactSound = ImpactSounds.RandomOrDefault(Game.CosmeticRandom);
			if (impactSound != null)
				Game.Sound.Play(impactSound, pos);
		}

		public bool IsValidImpact(WPos pos, Actor firedBy)
		{
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);
			if (!world.Map.Contains(targetTile))
				return false;

			var impactType = GetImpactType(world, targetTile, pos, firedBy);
			if (!ValidImpactTypes.HasFlag(impactType) || InvalidImpactTypes.HasFlag(impactType))
				return false;

			return true;
		}
	}
}
