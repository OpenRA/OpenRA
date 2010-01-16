using System.Collections.Generic;
using System.Linq;
using OpenRa.Mods.Aftermath.Orders;
using OpenRa.Traits;
using OpenRa.Traits.Activities;

namespace OpenRa.Mods.Aftermath
{
	class ChronoshiftDeployInfo : ITraitInfo
	{
		public object Create(Actor self) { return new ChronoshiftDeploy(self); }
	}

	class ChronoshiftDeploy : IIssueOrder, IResolveOrder, ITick, IPips
	{
		// Recharge logic
		[Sync]
		int chargeTick = 0; // How long until we can chronoshift again?
		readonly int chargeLength = (int)(Rules.Aftermath.ChronoTankDuration * 60 * 25); // How long between shifts?

		public ChronoshiftDeploy(Actor self) { }

		public void Tick(Actor self)
		{
			if (chargeTick > 0)
				chargeTick--;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Right && xy == self.Location && chargeTick <= 0)
				return new Order("Deploy", self, null, int2.Zero, null);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy")
			{
				Game.controller.orderGenerator = new SetChronoTankDestination(self);
				return;
			}

			var movement = self.traits.GetOrDefault<IMovement>();
			if (order.OrderString == "ChronoshiftSelf" && movement.CanEnterCell(order.TargetLocation))
			{
				// Cannot chronoshift into unexplored location
				if (!self.Owner.Shroud.IsExplored(order.TargetLocation))
					return;

				Game.controller.CancelInputMode();
				self.CancelActivity();
				self.QueueActivity(new Teleport(order.TargetLocation));
				Sound.Play("chrotnk1.aud");
				chargeTick = chargeLength;

				foreach (var a in Game.world.Actors.Where(a => a.traits.Contains<ChronoshiftPaletteEffect>()))
					a.traits.Get<ChronoshiftPaletteEffect>().DoChronoshift();
			}
		}

		// Display 5 pips indicating the current charge status
		public IEnumerable<PipType> GetPips(Actor self)
		{
			const int numPips = 5;
			for (int i = 0; i < numPips; i++)
			{
				if ((1 - chargeTick * 1.0f / chargeLength) * numPips < i + 1)
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
