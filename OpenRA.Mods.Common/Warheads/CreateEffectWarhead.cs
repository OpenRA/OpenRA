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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class CreateEffectWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("List of explosion sequences that can be used.")]
		[SequenceReference("Image")] public readonly string[] Explosions = new string[0];

		[Desc("Image containing explosion effect sequence.")]
		public readonly string Image = "explosion";

		[Desc("Palette to use for explosion effect."), PaletteReference("UsePlayerPalette")]
		public readonly string ExplosionPalette = "effect";

		[Desc("Remap explosion effect to player color, if art supports it.")]
		public readonly bool UsePlayerPalette = false;

		[Desc("Display explosion effect at ground level, regardless of explosion altitude.")]
		public readonly bool ForceDisplayAtGroundLevel = false;

		[Desc("List of sounds that can be played on impact.")]
		public readonly string[] ImpactSounds = new string[0];

		[Desc("Consider explosion above this altitude an air explosion.",
			"If that's the case, this warhead will consider the explosion position to have the 'Air' TargetType (in addition to any nearby actor's TargetTypes).")]
		public readonly WDist AirThreshold = new WDist(128);

		[Desc("Scan radius for victims around impact. If set to a negative value (default), it will automatically scale to the largest health shape.",
			"Custom overrides should not be necessary under normal circumstances.")]
		public WDist VictimScanRadius = new WDist(-1);

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (VictimScanRadius < WDist.Zero)
				VictimScanRadius = Util.MinimumRequiredVictimScanRadius(rules);
		}

		static readonly string[] TargetTypeAir = new string[] { "Air" };

		public ImpactType GetImpactType(World world, CPos cell, WPos pos, Actor firedBy)
		{
			// Matching target actor
			if (VictimScanRadius > WDist.Zero)
			{
				var targetType = GetDirectHitTargetType(world, cell, pos, firedBy, true);
				if (targetType == ImpactTargetType.ValidActor)
					return ImpactType.TargetHit;
				if (targetType == ImpactTargetType.InvalidActor)
					return ImpactType.None;
			}

			var dat = world.Map.DistanceAboveTerrain(pos);
			if (dat > AirThreshold)
				return ImpactType.Air;

			return ImpactType.Ground;
		}

		public ImpactTargetType GetDirectHitTargetType(World world, CPos cell, WPos pos, Actor firedBy, bool checkTargetValidity = false)
		{
			var victims = world.FindActorsInCircle(pos, VictimScanRadius);
			var invalidHit = false;

			foreach (var victim in victims)
			{
				if (!AffectsParent && victim == firedBy)
					continue;

				if (!victim.Info.HasTraitInfo<HealthInfo>())
					continue;

				// If the impact position is within any HitShape, we have a direct hit
				var activeShapes = victim.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				var directHit = activeShapes.Any(i => i.Info.Type.DistanceFromEdge(pos, victim).Length <= 0);

				// If the warhead landed outside the actor's hit-shape(s), we need to skip the rest so it won't be considered an invalidHit
				if (!directHit)
					continue;

				if (!checkTargetValidity || IsValidAgainst(victim, firedBy))
					return ImpactTargetType.ValidActor;

				// If we got here, it must be an invalid target
				invalidHit = true;
			}

			// If there was at least a single direct hit, but none on valid target(s), we return InvalidActor
			return invalidHit ? ImpactTargetType.InvalidActor : ImpactTargetType.NoActor;
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
			{
				if (ForceDisplayAtGroundLevel)
				{
					var dat = world.Map.DistanceAboveTerrain(pos);
					pos = new WPos(pos.X, pos.Y, pos.Z - dat.Length);
				}

				world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, w, Image, explosion, palette)));
			}

			var impactSound = ImpactSounds.RandomOrDefault(Game.CosmeticRandom);
			if (impactSound != null)
				Game.Sound.Play(SoundType.World, impactSound, pos);
		}

		public bool IsValidImpact(WPos pos, Actor firedBy)
		{
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);
			if (!world.Map.Contains(targetTile))
				return false;

			var impactType = GetImpactType(world, targetTile, pos, firedBy);
			switch (impactType)
			{
				case ImpactType.TargetHit:
					return true;
				case ImpactType.Air:
					return IsValidTarget(TargetTypeAir);
				case ImpactType.Ground:
					var tileInfo = world.Map.GetTerrainInfo(targetTile);
					return IsValidTarget(tileInfo.TargetTypes);
				default:
					return false;
			}
		}
	}
}
