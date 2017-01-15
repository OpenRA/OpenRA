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

using System;
using System.Linq;
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckTargetHealthRadius : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			var validActors = rules.Actors.Where(a => a.Value.TraitInfos<HealthInfo>().Any()).ToList();

			// TODO: Make this handle multiple Health traits per actor
			var largestHealthRadius = validActors.Max(a => a.Value.TraitInfo<HealthInfo>().Shape.OuterRadius.Length);

			foreach (var actorInfo in validActors)
			{
				var healthTraits = actorInfo.Value.TraitInfos<HealthInfo>().ToList();
				if (!healthTraits.Any())
					continue;

				var targetable = actorInfo.Value.TraitInfos<ITargetableInfo>().SelectMany(x => x.GetTargetTypes()).ToList();
				if (!targetable.Any())
					continue;

				var blockerTraits = actorInfo.Value.TraitInfos<BlocksProjectilesInfo>().ToList();
				var gate = actorInfo.Value.TraitInfoOrDefault<GateInfo>();

				foreach (var weaponInfo in rules.Weapons)
				{
					var warheads = weaponInfo.Value.Warheads.OfType<SpreadDamageWarhead>().Where(dw => dw.Damage > 0);

					foreach (var warhead in warheads)
					{
						// This is a special warhead, like the one on `weathering` in D2k.
						if (!warhead.DamageTypes.Any())
							continue;

						// This warhead cannot affect this actor.
						if (!warhead.ValidTargets.Overlaps(targetable))
							continue;

						var warheadExtraScanRadius = warhead.TargetExtraSearchRadius != WDist.Zero ? warhead.TargetExtraSearchRadius.Length : largestHealthRadius;
						var victimScanRadius = warhead.Range[warhead.Range.Length - 1].Length + warheadExtraScanRadius;
						if (healthTraits.Where(x => x.Shape.OuterRadius.Length > victimScanRadius).Any())
							emitError("Actor type `{0}` has a health radius exceeding the victim scan radius of `{1}` of a SpreadDamageWarhead on `{2}`!"
								.F(actorInfo.Key, victimScanRadius.ToString(), weaponInfo.Key));
					}

					var effectWarheads = weaponInfo.Value.Warheads.OfType<CreateEffectWarhead>();

					foreach (var warhead in effectWarheads)
					{
						// This warhead cannot affect this actor.
						if (!warhead.ValidTargets.Overlaps(targetable))
							continue;

						var warheadScanRadius = warhead.TargetSearchRadius != WDist.Zero ? warhead.TargetSearchRadius.Length : largestHealthRadius;
						if (healthTraits.Where(x => x.Shape.OuterRadius.Length > warheadScanRadius).Any())
							emitError("Actor type `{0}` has a health radius exceeding the victim scan radius of `{1}` a CreateEffectWarhead on `{2}`!"
								.F(actorInfo.Key, warheadScanRadius.ToString(), weaponInfo.Key));
					}

					var bullet = weaponInfo.Value.Projectile as BulletInfo;
					var instant = weaponInfo.Value.Projectile as InstantHitInfo;
					var missile = weaponInfo.Value.Projectile as MissileInfo;
					var areabeam = weaponInfo.Value.Projectile as AreaBeamInfo;
					var laserzap = weaponInfo.Value.Projectile as LaserZapInfo;

					// No blockable projectile
					if (bullet == null && instant == null && missile == null && areabeam == null && laserzap == null)
						continue;

					// Projectile is not a bullet and actor is not a blocker
					var actorIsBlocker = blockerTraits.Any() || (gate != null && gate.BlocksProjectilesHeight > 0);
					if (bullet == null && !actorIsBlocker)
						continue;

					var projectileScanRadius = 0;

					if (bullet != null)
					{
						if (!bullet.Blockable && bullet.BounceCount == 0)
							continue;

						var targetExtraSearchRadius = bullet.TargetExtraSearchRadius > WDist.Zero ? bullet.TargetExtraSearchRadius.Length : largestHealthRadius;
						projectileScanRadius = bullet.Width.Length + targetExtraSearchRadius;
					}

					if (instant != null)
					{
						if (!instant.Blockable)
							continue;

						var targetExtraSearchRadius = instant.TargetExtraSearchRadius > WDist.Zero ? instant.TargetExtraSearchRadius.Length : largestHealthRadius;
						projectileScanRadius = instant.Width.Length + targetExtraSearchRadius;
					}

					if (missile != null)
					{
						if (!missile.Blockable)
							continue;

						var targetExtraSearchRadius = missile.TargetExtraSearchRadius > WDist.Zero ? missile.TargetExtraSearchRadius.Length : largestHealthRadius;
						projectileScanRadius = missile.Width.Length + targetExtraSearchRadius;
					}

					if (areabeam != null)
					{
						if (!areabeam.Blockable)
							continue;

						var targetExtraSearchRadius = areabeam.TargetExtraSearchRadius > WDist.Zero ? areabeam.TargetExtraSearchRadius.Length : largestHealthRadius;
						projectileScanRadius = areabeam.Width.Length + targetExtraSearchRadius;
					}

					if (laserzap != null)
					{
						if (!laserzap.Blockable)
							continue;

						var targetExtraSearchRadius = laserzap.TargetExtraSearchRadius > WDist.Zero ? laserzap.TargetExtraSearchRadius.Length : largestHealthRadius;
						projectileScanRadius = laserzap.Width.Length + targetExtraSearchRadius;
					}

					if (healthTraits.Where(x => x.Shape.OuterRadius.Length > projectileScanRadius).Any())
						emitError("Actor type `{0}` has a health radius exceeding the victim scan radius of `{1}` on the projectile of `{2}`!"
							.F(actorInfo.Key, projectileScanRadius.ToString(), weaponInfo.Key));
				}
			}
		}
	}
}
