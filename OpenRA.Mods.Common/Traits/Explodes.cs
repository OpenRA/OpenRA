#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ExplodesInfo : ITraitInfo
	{
		[WeaponReference]
		public readonly string Weapon = "UnitExplode";
		[WeaponReference]
		public readonly string EmptyWeapon = "UnitExplode";

		public readonly int Chance = 100;
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

			if (info.DeathType != null && e.Warhead != null && !info.DeathType.Intersect(e.Warhead.DamageTypes).Any())
				return;

			var weaponName = ChooseWeaponForExplosion(self);
			if (weaponName == null)
				return;

			var weapon = e.Attacker.World.Map.Rules.Weapons[weaponName.ToLowerInvariant()];
			if (weapon.Report != null && weapon.Report.Any())
				Sound.Play(weapon.Report.Random(e.Attacker.World.SharedRandom), self.CenterPosition);

			// Use .FromPos since this actor is killed. Cannot use Target.FromActor
			weapon.Impact(Target.FromPos(self.CenterPosition), e.Attacker, Enumerable.Empty<int>());
		}

		string ChooseWeaponForExplosion(Actor self)
		{
			var shouldExplode = self.TraitsImplementing<IExplodeModifier>().All(a => a.ShouldExplode(self));
			return shouldExplode ? info.Weapon : info.EmptyWeapon;
		}
	}
}
