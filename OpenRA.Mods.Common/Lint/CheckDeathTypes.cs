#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckDeathTypes : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			foreach (var actorInfo in map.Rules.Actors)
			{
				var animations = actorInfo.Value.Traits.WithInterface<WithDeathAnimationInfo>().ToList();
				if (!animations.Any())
					continue;

				var deathTypes = animations.SelectMany(x => x.DeathTypes.Select(y => y.Key)).ToList();
				if (!deathTypes.Any())
					continue;

				var targetable = actorInfo.Value.Traits.WithInterface<ITargetableInfo>().SelectMany(x => x.GetTargetTypes()).ToList();
				if (!targetable.Any())
					continue;

				foreach (var weaponInfo in map.Rules.Weapons)
				{
					var warheads = weaponInfo.Value.Warheads.OfType<DamageWarhead>().Where(dw => dw.Damage > 0);

					foreach (var warhead in warheads)
					{
						// This is a special warhead, like the one on `weathering` in D2k.
						if (!warhead.DamageTypes.Any())
							continue;

						// This warhead cannot affect this actor.
						if (!warhead.ValidTargets.Intersect(targetable).Any())
							continue;

						if (!warhead.DamageTypes.Intersect(deathTypes).Any())
							emitError("Actor type `{0}` does not define a death animation for weapon `{1}`!"
								.F(actorInfo.Key, weaponInfo.Key));
					}
				}
			}
		}
	}
}
