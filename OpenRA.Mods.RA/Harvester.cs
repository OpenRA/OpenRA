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
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class HarvesterInfo : ITraitInfo
	{
		public readonly int Capacity = 28;
		public readonly int PipCount = 7;
		public readonly string[] Resources = { };

		public object Create(Actor self) { return new Harvester(self); }
	}

	public class Harvester : IIssueOrder, IResolveOrder, IPips
	{
		Dictionary<ResourceTypeInfo, int> contents = new Dictionary<ResourceTypeInfo, int>();
		
		Actor self;
		public Harvester(Actor self)
		{
			this.self = self;
		}

		public bool IsFull { get { return contents.Values.Sum() == self.Info.Traits.Get<HarvesterInfo>().Capacity; } }
		public bool IsEmpty { get { return contents.Values.Sum() == 0; } }

		public void AcceptResource(ResourceTypeInfo type)
		{
			if (!contents.ContainsKey(type)) contents[type] = 1;
			else contents[type]++;
		}

		public void Deliver(Actor self, Actor proc)
		{
			proc.Owner.GiveOre(contents.Sum(kv => kv.Key.ValuePerUnit * kv.Value));
			contents.Clear();
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor != null
				&& underCursor.Owner == self.Owner
				&& underCursor.traits.Contains<IAcceptOre>() && !IsEmpty)
				return new Order("Deliver", self, underCursor);

			var res = self.World.WorldActor.traits.Get<ResourceLayer>().GetResource(xy);
			var info = self.Info.Traits.Get<HarvesterInfo>();

			if (underCursor == null && res != null && info.Resources.Contains(res.Name))
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
				self.QueueActivity(new DeliverResources(order.TargetActor));
			}
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			int numPips = self.Info.Traits.Get<HarvesterInfo>().PipCount;
			int n = contents.Values.Sum();

			for (int i = 0; i < numPips; i++)
			{
				// todo: pip colors based on ResourceTypeInfo
				if (n * 1.0f / self.Info.Traits.Get<HarvesterInfo>().Capacity > i * 1.0f / numPips)
					yield return PipType.Yellow;
				else
					yield return PipType.Transparent;
			}
		}
	}
}
