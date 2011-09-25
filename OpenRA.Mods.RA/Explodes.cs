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
	class ExplodesInfo : TraitInfo<Explodes>
	{
		[WeaponReference]
		public readonly string Weapon = "UnitExplode";
		[WeaponReference]
		public readonly string EmptyWeapon = "UnitExplode";

		public readonly int Chance = 100;
	}

	class Explodes : INotifyKilled
	{
		public void Killed(Actor self, AttackInfo e)
		{
			if (self.World.SharedRandom.Next(100) > self.Info.Traits.Get<ExplodesInfo>().Chance)
				return;

			var weapon = ChooseWeaponForExplosion(self);
			if (weapon != null)
			{
				var move = self.TraitOrDefault<IMove>();
				var altitude = move != null ? move.Altitude : 0;
				Combat.DoExplosion(e.Attacker, weapon, self.CenterLocation, altitude);
			}
		}

		string ChooseWeaponForExplosion(Actor self)
		{
			var shouldExplode = self.TraitsImplementing<IExplodeModifier>().All(a => a.ShouldExplode(self));

			var info = self.Info.Traits.Get<ExplodesInfo>();
			return shouldExplode ? info.Weapon : info.EmptyWeapon;
		}
	}
}
