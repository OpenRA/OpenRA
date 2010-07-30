#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
	}

	class Explodes : INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
				var weapon = ChooseWeaponForExplosion(self);
				if (weapon != null)
				{
					var unit = self.traits.GetOrDefault<Unit>();
					var altitude = unit != null ? unit.Altitude : 0;
					Combat.DoExplosion(e.Attacker, weapon, self.CenterLocation, altitude);
				}
			}
		}

		string ChooseWeaponForExplosion(Actor self)
		{
			var shouldExplode = self.traits.WithInterface<IExplodeModifier>().All(a => a.ShouldExplode(self));

			var info = self.Info.Traits.Get<ExplodesInfo>();
			return shouldExplode ? info.Weapon : info.EmptyWeapon;
		}
	}
}
