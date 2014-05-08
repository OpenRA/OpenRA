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
		public readonly string[] InfDeath = { };

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

			if (explodesInfo.InfDeath != null && e.Warhead != null && !explodesInfo.InfDeath.Contains(e.Warhead.InfDeath))
				return;

			var weapon = ChooseWeaponForExplosion(self);
			if (weapon != null)
				Combat.DoExplosion(e.Attacker, weapon, self.CenterPosition);
		}

		string ChooseWeaponForExplosion(Actor self)
		{
			var shouldExplode = self.TraitsImplementing<IExplodeModifier>().All(a => a.ShouldExplode(self));
			return shouldExplode ? explodesInfo.Weapon : explodesInfo.EmptyWeapon;
		}
	}
}
