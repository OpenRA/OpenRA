#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ExplodesInfo : ITraitInfo
	{
		[WeaponReference]
		public readonly string Weapon = "UnitExplode";
		[WeaponReference]
		public readonly string EmptyWeapon = "UnitExplode";

		public readonly int Chance = 100;
		public readonly string[] DeathType = null;

		public object Create(ActorInitializer init) { return new Explodes(this); }
	}

	class Explodes : INotifyKilled
	{
		readonly ExplodesInfo explodesInfo;

		public Explodes(ExplodesInfo info) { explodesInfo = info; }

		public void Killed(Actor self, AttackInfo e)
		{
			if (!self.IsInWorld)
				return;

			if (self.World.SharedRandom.Next(100) > explodesInfo.Chance)
				return;

			if (explodesInfo.DeathType != null && e.Warhead != null && !explodesInfo.DeathType.Contains(e.Warhead.DeathType))
				return;

			var weaponName = ChooseWeaponForExplosion(self);
			if (weaponName != null)
			{
				var weapon = e.Attacker.World.Map.Rules.Weapons[weaponName.ToLowerInvariant()];
				if (weapon.Report != null && weapon.Report.Any())
					Sound.Play(weapon.Report.Random(e.Attacker.World.SharedRandom), self.CenterPosition);

				// Use .FromPos since this actor is killed. Cannot use Target.FromActor
				weapon.Impact(Target.FromPos(self.CenterPosition), e.Attacker, Enumerable.Empty<int>());
			}
		}

		string ChooseWeaponForExplosion(Actor self)
		{
			var shouldExplode = self.TraitsImplementing<IExplodeModifier>().All(a => a.ShouldExplode(self));
			return shouldExplode ? explodesInfo.Weapon : explodesInfo.EmptyWeapon;
		}
	}
}
