using OpenRa.Game.Traits;
using OpenRa.Game.Orders;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace OpenRa.Game.Traits
{
	class Chronoshiftable : IOrder, ISpeedModifier, ITick
	{
		// Return-to-sender logic
		int2 chronoshiftOrigin;
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

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return null; // Chronoshift order is issued through Chrome.
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ChronosphereSelect")
			{
				Game.controller.orderGenerator = new ChronoshiftDestinationOrderGenerator(self);
			}

			var movement = self.traits.WithInterface<IMovement>().FirstOrDefault();
			if (order.OrderString == "Chronoshift" && movement.CanEnterCell(order.TargetLocation))
			{
				chronoshiftOrigin = self.Location;
				chronoshiftReturnTicks = (int)(Rules.General.ChronoDuration * 60 * 25);
				// TODO: Kill cargo if Rules.General.ChronoKillCargo says so
				Game.controller.CancelInputMode();
				self.CancelActivity();
				self.QueueActivity(new Activities.Teleport(order.TargetLocation));
				Sound.Play("chrono2.aud");

				foreach (var a in Game.world.Actors.Where(a => a.traits.Contains<ChronoshiftPaletteEffect>()))
					a.traits.Get<ChronoshiftPaletteEffect>().DoChronoshift();
			}
		}

		public float GetSpeedModifier()
		{
			// ARGH! You must not do this, it will desync!
			return (Game.controller.orderGenerator is ChronoshiftDestinationOrderGenerator) ? 0f : 1f;
		}
	}
}
