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

	class DemoTruck : Chronoshiftable, IResolveOrder, INotifyDamage
	{
		public DemoTruck(Actor self) : base(self) { }

		public new void ResolveOrder(Actor self, Order order)
		{
			// Override chronoshifting action to detonate vehicle
			var movement = self.traits.GetOrDefault<IMovement>();
			var chronosphere = self.World.Actors.Where(a => a.Owner == order.Subject.Owner && a.traits.Contains<Chronosphere>()).FirstOrDefault();
			if (order.OrderString == "Chronoshift" && movement.CanEnterCell(order.TargetLocation))
			{
				self.InflictDamage(chronosphere, self.Health, Rules.WarheadInfo["Super"]);
				return;
			}

			base.ResolveOrder(self, order);
		}

		// Fire primary on death
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				Detonate(self, e.Attacker);
		}

		public void Detonate(Actor self, Actor detonatedBy)
		{
			self.InflictDamage(detonatedBy, self.Health, Rules.WarheadInfo["Super"]);
			var unit = self.traits.GetOrDefault<Unit>();
			var altitude = unit != null ? unit.Altitude : 0;
			int2 detonateLocation = self.CenterLocation.ToInt2();

			self.World.AddFrameEndTask(
				w => w.Add(new Bullet(self.Info.Traits.Get<AttackBaseInfo>().PrimaryWeapon, detonatedBy.Owner, detonatedBy,
					detonateLocation, detonateLocation, altitude, altitude)));
		}
	}
}
