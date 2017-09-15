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
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class TransferAmmoWarhead : Warhead
	{
		[Desc("Name of the AmmoPool.")]
		public readonly string AmmoPoolName = "primary";

		[Desc("Give ammo to source actor if the target actor doesn't have enough space.")]
		public readonly bool ReturnAmmo = true;

		[Desc("Amount of ammo to transfer from the source to the target (0 = consume 0 and provide 1)")]
		public readonly int Amount = 0;

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
			var amountOfThisShot = 0;

			if (Amount == 0)
			{
				// Ammo was already consumed by the armament.
				amountOfThisShot = 1;
			}
			else
			{
				foreach (var pool in GetAmmoPools(firedBy).Where(x => x.HasAmmo()))
				{
					var amountFromThisPool = Math.Min(Amount - amountOfThisShot, pool.GetAmmoCount());
					amountOfThisShot += amountFromThisPool;
					pool.AddAmmo(-amountFromThisPool);

					if (amountOfThisShot == Amount)
						break;
				}
			}

			if (IsValidAgainst(victim, firedBy))
			{
				foreach (var pool in GetAmmoPools(victim).Where(x => !x.FullAmmo()))
				{
					var amountToThisPool = Math.Min(amountOfThisShot, pool.Info.Ammo - pool.GetAmmoCount());
					amountOfThisShot -= amountToThisPool;
					pool.AddAmmo(amountToThisPool);

					if (amountOfThisShot == 0)
						break;
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

			if (ReturnAmmo && amountOfThisShot > 0)
			{
				foreach (var pool in GetAmmoPools(firedBy).Where(x => !x.FullAmmo()))
				{
					var amountToThisPool = Math.Min(amountOfThisShot, pool.Info.Ammo - pool.GetAmmoCount());
					amountOfThisShot -= amountToThisPool;
					pool.AddAmmo(amountToThisPool);

					if (amountOfThisShot == 0)
						break;
				}
			}
		}

		IEnumerable<AmmoPool> GetAmmoPools(Actor actor)
		{
			return actor.TraitsImplementing<AmmoPool>().Where(x => x.Info.Name == AmmoPoolName);
		}
	}
}
