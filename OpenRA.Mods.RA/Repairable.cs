#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class RepairableInfo : TraitInfo<Repairable> { public readonly string[] RepairBuildings = { "fix" }; }

	class Repairable : IIssueOrder, IResolveOrder, IOrderCursor
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;

			if (self.Info.Traits.Get<RepairableInfo>().RepairBuildings.Contains(underCursor.Info.Name)
				&& underCursor.Owner == self.Owner)
				return new Order("Enter", self, underCursor);

			return null;
		}

		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Enter") ? "enter" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Enter")
			{
                var rp = order.TargetActor.traits.GetOrDefault<RallyPoint>();

				self.CancelActivity();
				self.QueueActivity(new Move(Util.CellContaining(order.TargetActor.CenterLocation), order.TargetActor));
				self.QueueActivity(new Rearm());
				self.QueueActivity(new Repair(order.TargetActor));

				if (rp != null)
					self.QueueActivity(new CallFunc(
						() => self.QueueActivity(new Move(rp.rallyPoint, order.TargetActor))));
			}
		}
	}
}
