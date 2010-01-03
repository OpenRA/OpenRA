using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Orders;

namespace OpenRa.Game.Traits
{
	class ChronoshiftDeploy : IOrder, ISpeedModifier, ITick, IPips
	{
		public ChronoshiftDeploy(Actor self) { }
		int remainingChargeTime = 0; // How long until we can chronoshift again?
		int chargeTime = (int)(Rules.Aftermath.ChronoTankDuration * 60 * 25); // How long between shifts?

		public void Tick(Actor self)
		{
			if (remainingChargeTime > 0)
				remainingChargeTime--;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			else if (xy == self.Location && remainingChargeTime <= 0)
				return new Order("Deploy", self, null, int2.Zero, null);
 
			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy" && remainingChargeTime <= 0)
			{
				Game.controller.orderGenerator = new TeleportOrderGenerator(self);
				self.CancelActivity();
			}

			var movement = self.traits.WithInterface<IMovement>().FirstOrDefault();
			if (order.OrderString == "Chronoshift" && movement.CanEnterCell(order.TargetLocation))
			{
				Game.controller.CancelInputMode();
				self.CancelActivity();
				self.QueueActivity(new Activities.Teleport(order.TargetLocation));
				Sound.Play("chrotnk1.aud");
				remainingChargeTime = chargeTime;
			}
		}

		public float GetSpeedModifier()
		{
			return (Game.controller.orderGenerator is TeleportOrderGenerator) ? 0f : 1f;
		}

		// Display 5 pips indicating the current charge status
		public IEnumerable<PipType> GetPips()
		{
			const int numPips = 5;
			for (int i = 0; i < numPips; i++)
			{
				if ((1 - remainingChargeTime * 1.0f / chargeTime) * numPips < i + 1)
				{
					yield return PipType.Transparent;
					continue;
				}
					
				switch (i)
				{
					case 0:
					case 1:
						yield return PipType.Red;
						break;
					case 2:
					case 3:
						yield return PipType.Yellow;
						break;
					case 4:
						yield return PipType.Green;
						break;
				}
			}
		}
	}
}
