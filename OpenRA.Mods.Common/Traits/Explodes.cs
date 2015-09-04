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
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor explodes when killed.")]
	public class ExplodesInfo : ITraitInfo
	{
		[WeaponReference, FieldLoader.Require, Desc("Weapon to use for explosion if ammo/payload is loaded.")]
		public readonly string Weapon = "UnitExplode";

		[WeaponReference, Desc("Weapon to use for explosion if no ammo/payload is loaded.")]
		public readonly string EmptyWeapon = "UnitExplode";

		[Desc("Chance that the explosion will use Weapon if the actor has ammo/payload.")]
		public readonly int LoadedChance = 100;

		[Desc("Chance that this actor will explode at all.")]
		public readonly int Chance = 100;

		[Desc("DeathType(s) to apply upon explosion.")]
		public readonly string[] DeathType = null;

		public object Create(ActorInitializer init) { return new Explodes(this); }
	}

	public class Explodes : INotifyKilled
	{
		readonly ExplodesInfo info;

		public Explodes(ExplodesInfo info) { this.info = info; }

		public void Killed(Actor self, AttackInfo e)
		{
			if (!self.IsInWorld)
				return;

			if (self.World.SharedRandom.Next(100) > info.Chance)
				return;

			var warhead = e.Warhead as DamageWarhead;
			if (info.DeathType != null && warhead != null && !warhead.DamageTypes.Overlaps(info.DeathType))
				return;

			var weaponName = ChooseWeaponForExplosion(self);
			if (string.IsNullOrEmpty(weaponName))
				return;

			if (!e.Attacker.World.Map.Rules.Weapons.ContainsKey(weaponName.ToLowerInvariant()))
				throw new InvalidOperationException("Actor " + self.Info.Name
					+ ": Could not find weapon '" + weaponName.ToLowerInvariant() + "', check for typos.");

			var weapon = e.Attacker.World.Map.Rules.Weapons[weaponName.ToLowerInvariant()];
			if (weapon.Report != null && weapon.Report.Any())
				Sound.Play(weapon.Report.Random(e.Attacker.World.SharedRandom), self.CenterPosition);

			// Use .FromPos since this actor is killed. Cannot use Target.FromActor
			weapon.Impact(Target.FromPos(self.CenterPosition), e.Attacker, Enumerable.Empty<int>());
		}

		string ChooseWeaponForExplosion(Actor self)
		{
			var shouldExplode = self.TraitsImplementing<IExplodeModifier>().All(a => a.ShouldExplode(self));
			var useFullExplosion = self.World.SharedRandom.Next(100) <= info.LoadedChance;
			return (shouldExplode && useFullExplosion) ? info.Weapon : info.EmptyWeapon;
		}
	}
}
