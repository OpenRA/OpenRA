using OpenRa.Game.Effects;
using OpenRa.Game.Traits;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Orders;

namespace OpenRa.Game.Traits
{
	class DemoTruck : IOrder, ISpeedModifier, INotifyDamage, IChronoshiftable
	{
		readonly Actor self;
		public DemoTruck(Actor self)
		{
			this.self = self;
		}
		
		// Fire primary on Chronoshift
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return null; // Chronoshift order is issued through Chrome.
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ChronosphereSelect")
				Game.controller.orderGenerator = new ChronoshiftDestinationOrderGenerator(self);
			
			var movement = self.traits.WithInterface<IMovement>().FirstOrDefault();
			var chronosphere = Game.world.Actors.Where(a => a.Owner == order.Subject.Owner && a.traits.Contains<Chronosphere>()).FirstOrDefault();
			if (order.OrderString == "Chronoshift" && movement.CanEnterCell(order.TargetLocation))
				self.InflictDamage(chronosphere, self.Health, Rules.WarheadInfo["Super"]);
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
			
			Game.world.AddFrameEndTask(
				w => w.Add(new Bullet(self.Info.Primary, detonatedBy.Owner, detonatedBy,
					detonateLocation, detonateLocation,	altitude, altitude)));
		}

		public float GetSpeedModifier()
		{
			// ARGH! You must not do this, it will desync!
			return (Game.controller.orderGenerator is ChronoshiftDestinationOrderGenerator) ? 0f : 1f;
		}
	}
}
