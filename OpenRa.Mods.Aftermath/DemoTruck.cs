using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits;
using OpenRa.Effects;

namespace OpenRa.Mods.Aftermath
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
