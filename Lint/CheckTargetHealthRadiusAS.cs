#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.AS.Warheads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Lint
{
	class CheckTargetHealthRadiusAS : ILintRulesPass
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
					var warheadAS = weaponInfo.Value.Warheads.OfType<WarheadAS>();

					foreach (var wh in warheadAS)
					{
						// This warhead cannot affect this actor.
						if (!wh.ValidTargets.Overlaps(targetable))
							continue;

						if (healthTraits.Any(x => x.Shape.OuterRadius.Length > wh.TargetSearchRadius.Length))
							emitError("Actor type `{0}` has a health radius exceeding the victim scan radius of an AS warhead on `{1}`!"
								.F(actorInfo.Key, weaponInfo.Key));
					}
				}
			}
		}
	}
}
