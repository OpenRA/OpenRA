#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
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
		public readonly int[] InfDeath = null;

		public object Create (ActorInitializer init) { return new Explodes(this); }
	}

	class Explodes : INotifyKilled
	{
		readonly ExplodesInfo Info;

		public Explodes( ExplodesInfo info ) { Info = info; }

		public void Killed(Actor self, AttackInfo e)
		{
			if (self.World.SharedRandom.Next(100) > Info.Chance)
				return;

			if (Info.InfDeath != null && e.Warhead != null && !Info.InfDeath.Contains(e.Warhead.InfDeath))
				return;

			var weapon = ChooseWeaponForExplosion(self);
			if (weapon != null)
				Combat.DoExplosion(e.Attacker, weapon, self.CenterPosition);
		}

		string ChooseWeaponForExplosion(Actor self)
		{
			var shouldExplode = self.TraitsImplementing<IExplodeModifier>().All(a => a.ShouldExplode(self));
			return shouldExplode ? Info.Weapon : Info.EmptyWeapon;
		}
	}
}
