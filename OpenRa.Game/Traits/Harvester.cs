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

using System.Collections.Generic;
using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class HarvesterInfo : ITraitInfo
	{
		public readonly int BailCount = 28;
		public readonly int PipCount = 7;

		public object Create(Actor self) { return new Harvester(self); }
	}

	public class Harvester : IIssueOrder, IResolveOrder, IPips
	{
		[Sync]
		public int oreCarried = 0;					/* sum of these must not exceed capacity */
		[Sync]
		public int gemsCarried = 0;
		
		Actor self;
		public Harvester(Actor self)
		{
			this.self = self;
		}

		public bool IsFull { get { return oreCarried + gemsCarried == self.Info.Traits.Get<HarvesterInfo>().BailCount; } }
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
				&& underCursor.traits.Contains<IAcceptOre>() && !IsEmpty)
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
			int numPips = self.Info.Traits.Get<HarvesterInfo>().PipCount;

			for (int i = 0; i < numPips; i++)
			{
				if (gemsCarried * 1.0f / self.Info.Traits.Get<HarvesterInfo>().BailCount > i * 1.0f / numPips)
				{
					yield return PipType.Red;
					continue;
				}

				if ((gemsCarried + oreCarried) * 1.0f / self.Info.Traits.Get<HarvesterInfo>().BailCount > i * 1.0f / numPips)
				{
					yield return PipType.Yellow;
					continue;
				}
				yield return PipType.Transparent;
			}
		}
	}
}
