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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckDeathTypes : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				var animations = actorInfo.Value.TraitInfos<WithDeathAnimationInfo>().ToList();
				if (!animations.Any())
					continue;

				var deathAnimationDeathtypes = animations.SelectMany(x => x.DeathTypes.Select(y => y.Key)).ToList();
				var spawnActorDeathtypes = actorInfo.Value.TraitInfos<SpawnActorOnDeathInfo>().Where(s => !string.IsNullOrEmpty(s.DeathType)).Select(a => a.DeathType);
				var deathTypes = deathAnimationDeathtypes.Concat(spawnActorDeathtypes).Distinct();
				if (!deathTypes.Any())
					continue;

				var targetable = actorInfo.Value.TraitInfos<ITargetableInfo>().SelectMany(x => x.GetTargetTypes()).ToList();
				if (!targetable.Any())
					continue;

				foreach (var weaponInfo in rules.Weapons)
				{
					var warheads = weaponInfo.Value.Warheads.OfType<DamageWarhead>().Where(dw => dw.Damage > 0);

					foreach (var warhead in warheads)
					{
						// This is a special warhead, like the one on `weathering` in D2k.
						if (!warhead.DamageTypes.Any())
							continue;

						// This warhead cannot affect this actor.
						if (!warhead.ValidTargets.Overlaps(targetable))
							continue;

						if (!warhead.DamageTypes.Overlaps(deathTypes))
							emitError("Actor type {0} doesn't define a death animation or spawn an actor on death for weapon {1}!"
								.F(actorInfo.Key, weaponInfo.Key));
					}
				}
			}
		}
	}
}
