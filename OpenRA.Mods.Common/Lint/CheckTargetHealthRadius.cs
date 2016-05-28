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
			foreach (var actorInfo in rules.Actors)
			{
				var healthTraits = actorInfo.Value.TraitInfos<HealthInfo>().ToList();
				if (!healthTraits.Any())
					continue;

				var targetable = actorInfo.Value.TraitInfos<ITargetableInfo>().SelectMany(x => x.GetTargetTypes()).ToList();
				if (!targetable.Any())
					continue;

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

						if (healthTraits.Where(x => x.Shape.OuterRadius.Length > warhead.TargetExtraSearchRadius.Length).Any())
							emitError("Actor type `{0}` has a health radius exceeding the victim scan radius of a SpreadDamageWarhead on `{1}`!"
								.F(actorInfo.Key, weaponInfo.Key));
					}

					var effectWarheads = weaponInfo.Value.Warheads.OfType<CreateEffectWarhead>();

					foreach (var warhead in effectWarheads)
					{
						// This warhead cannot affect this actor.
						if (!warhead.ValidTargets.Overlaps(targetable))
							continue;

						if (healthTraits.Where(x => x.Shape.OuterRadius.Length > warhead.TargetSearchRadius.Length).Any())
							emitError("Actor type `{0}` has a health radius exceeding the victim scan radius of a CreateEffectWarhead on `{1}`!"
								.F(actorInfo.Key, weaponInfo.Key));
					}

					var bullet = weaponInfo.Value.Projectile as BulletInfo;
					var missile = weaponInfo.Value.Projectile as MissileInfo;
					var areabeam = weaponInfo.Value.Projectile as AreaBeamInfo;

					if (bullet == null && missile == null && areabeam == null)
						continue;

					var targetExtraSearchRadius = WDist.Zero;

					if (bullet != null)
						targetExtraSearchRadius = bullet.TargetExtraSearchRadius;

					if (missile != null)
						targetExtraSearchRadius = missile.TargetExtraSearchRadius;

					if (areabeam != null)
						targetExtraSearchRadius = areabeam.TargetExtraSearchRadius;

					if (healthTraits.Where(x => x.Shape.OuterRadius.Length > targetExtraSearchRadius.Length).Any())
						emitError("Actor type `{0}` has a health radius exceeding the victim scan radius of the projectile on `{1}`!"
							.F(actorInfo.Key, weaponInfo.Key));
				}
			}
		}
	}
}
