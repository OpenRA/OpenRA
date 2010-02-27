#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Aftermath
{
	class DemoTruckInfo : ITraitInfo
	{
		public object Create(Actor self) { return new DemoTruck(self); }
	}

	class DemoTruck : Chronoshiftable, INotifyDamage
	{
		public DemoTruck(Actor self) : base(self) { }

		// Explode on chronoshift
		public override bool Activate(Actor self, int2 targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			Detonate(self, chronosphere);
			return false;
		}

		// Fire primary on death
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				Detonate(self, e.Attacker);
		}

		public void Detonate(Actor self, Actor detonatedBy)
		{
			var unit = self.traits.GetOrDefault<Unit>();
			var info = self.Info.Traits.Get<AttackBaseInfo>();
			var altitude = unit != null ? unit.Altitude : 0;
			int2 detonateLocation = self.CenterLocation.ToInt2();

			self.World.AddFrameEndTask( w =>
			{
				// Fire weapon		
				w.Add(new Bullet(info.PrimaryWeapon, detonatedBy.Owner, detonatedBy,
					detonateLocation, detonateLocation, altitude, altitude));
				
				var weapon = Rules.WeaponInfo[info.PrimaryWeapon];
				if (!string.IsNullOrEmpty(weapon.Report))
					Sound.Play(weapon.Report + ".aud");

				// Remove from world
				self.Health = 0;
				detonatedBy.Owner.Kills++;
				w.Remove(self);
			} );
		}
	}
}
