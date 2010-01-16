using OpenRa.Traits;
using OpenRa.Orders;
using System.Collections.Generic;
using System.Linq;

namespace OpenRa.Traits
{
	class ChronoshiftableInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Chronoshiftable(self); }
	}

	public class Chronoshiftable : IResolveOrder, ISpeedModifier, ITick
	{
		// Return-to-sender logic
		[Sync]
		int2 chronoshiftOrigin;
		[Sync]
		int chronoshiftReturnTicks = 0;

		public Chronoshiftable(Actor self) { }

		public void Tick(Actor self)
		{
			if (chronoshiftReturnTicks <= 0)
				return;

			if (chronoshiftReturnTicks > 0)
				chronoshiftReturnTicks--;

			// Return to original location
			if (chronoshiftReturnTicks == 0)
			{
				self.CancelActivity();
				// Todo: need a new Teleport method that will move to the closest available cell
				self.QueueActivity(new Activities.Teleport(chronoshiftOrigin));
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ChronosphereSelect")
			{
				var power = self.Owner.SupportPowers[order.TargetString];
				Game.controller.orderGenerator = new ChronoshiftDestinationOrderGenerator(self, power);
			}

			var movement = self.traits.GetOrDefault<IMovement>();
			if (order.OrderString == "Chronoshift" && movement.CanEnterCell(order.TargetLocation))
			{
				// Cannot chronoshift into unexplored location
				if (!self.Owner.Shroud.IsExplored(order.TargetLocation))
					return;
				
				// Set up return-to-sender info
				chronoshiftOrigin = self.Location;
				chronoshiftReturnTicks = (int)(Rules.General.ChronoDuration * 60 * 25);

				// Kill cargo
				if (Rules.General.ChronoKillCargo && self.traits.Contains<Cargo>())
				{
					var cargo = self.traits.Get<Cargo>();
					while (!cargo.IsEmpty(self))
					{
						order.Player.Kills++;
						cargo.Unload(self);
					}
				}
				
				// Set up the teleport
				self.CancelActivity();
				self.QueueActivity(new Activities.Teleport(order.TargetLocation));

				var power = self.Owner.SupportPowers[order.TargetString].Impl;
				power.OnFireNotification(self, self.Location);
			}
		}

		public float GetSpeedModifier()
		{
			// ARGH! You must not do this, it will desync!
			return (Game.controller.orderGenerator is ChronoshiftDestinationOrderGenerator) ? 0f : 1f;
		}
	}
}
