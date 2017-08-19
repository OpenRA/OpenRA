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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class ProvideAmmoWarhead : Warhead
	{
		[Desc("Name of the AmmoPool.")]
		public readonly string AmmoPoolName = "primary";

		[Desc("Give ammo to source actor if the target actor doesn't have enough space.")]
		public readonly bool ReturnAmmo = true;

		public override bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			if (!base.IsValidAgainst(victim, firedBy))
				return false;

			return GetAmmoPools(victim).Any(x => !x.FullAmmo());
		}

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			// Damages a single actor, rather than a position. Only support by InstantHit for now.
			// TODO: Add support for 'area of damage'
			if (target.Type == TargetType.Actor)
				DoImpact(target.Actor, firedBy, damageModifiers);
		}

		public void DoImpact(Actor victim, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			bool wasSuccessful = false;

			if (IsValidAgainst(victim, firedBy))
			{
				foreach (var pool in GetAmmoPools(victim).Where(x => !x.FullAmmo()))
				{
					if (pool.GiveAmmo())
					{
						wasSuccessful = true;
						break;
					}
				}

				var world = firedBy.World;
				if (world.LocalPlayer != null)
				{
					var debugOverlayRange = new[] { WDist.Zero, new WDist(128) };
					var debugVis = world.LocalPlayer.PlayerActor.TraitOrDefault<DebugVisualizations>();
					if (debugVis != null && debugVis.CombatGeometry)
						world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(victim.CenterPosition, debugOverlayRange, DebugOverlayColor);
				}
			}

			if (ReturnAmmo && !wasSuccessful)
			{
				foreach (var pool in GetAmmoPools(firedBy).Where(x => !x.FullAmmo()))
				{
					pool.GiveAmmo();
				}
			}
		}

		IEnumerable<AmmoPool> GetAmmoPools(Actor actor)
		{
			return actor.TraitsImplementing<AmmoPool>().Where(x => x.Info.Name == AmmoPoolName);
		}
	}
}
