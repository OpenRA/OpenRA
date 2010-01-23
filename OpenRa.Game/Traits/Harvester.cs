using System.Collections.Generic;
using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class HarvesterInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Harvester(); }
	}

	class Harvester : IIssueOrder, IResolveOrder, IPips
	{
		[Sync]
		public int oreCarried = 0;					/* sum of these must not exceed capacity */
		[Sync]
		public int gemsCarried = 0;

		public bool IsFull { get { return oreCarried + gemsCarried == Rules.General.BailCount; } }
		public bool IsEmpty { get { return oreCarried == 0 && gemsCarried == 0; } }

		public void AcceptResource(bool isGem)
		{
			if (isGem) gemsCarried++;
			else oreCarried++;
		}

		public void Deliver(Actor self, Actor proc)
		{
			proc.Owner.GiveOre(oreCarried * Rules.General.GoldValue);
			proc.Owner.GiveOre(gemsCarried * Rules.General.GemValue);
			oreCarried = 0;
			gemsCarried = 0;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor != null
				&& underCursor.Owner == self.Owner
				&& underCursor.traits.Contains<AcceptsOre>() && !IsEmpty)
				return new Order("Deliver", self, underCursor);

			if (underCursor == null && self.World.Map.ContainsResource(xy))
				return new Order("Harvest", self, xy);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Harvest")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetLocation, 0));
				self.QueueActivity(new Harvest());
			}
			else if (order.OrderString == "Deliver")
			{
				self.CancelActivity();
				self.QueueActivity(new DeliverOre(order.TargetActor));
			}
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			const int numPips = 7;
			for (int i = 0; i < numPips; i++)
			{
				if (gemsCarried * 1.0f / Rules.General.BailCount > i * 1.0f / numPips)
				{
					yield return PipType.Red;
					continue;
				}

				if ((gemsCarried + oreCarried) * 1.0f / Rules.General.BailCount > i * 1.0f / numPips)
				{
					yield return PipType.Yellow;
					continue;
				}
				yield return PipType.Transparent;
			}
		}
	}
}
