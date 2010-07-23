#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class EngineerRepairInfo : TraitInfo<EngineerRepair>
	{
		public readonly bool RepairsBridges = true;
	}

	class EngineerRepair : IIssueOrder, IResolveOrder, IOrderCursor
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (!CanRepair(self, underCursor)) return null;
			
			return new Order("EngineerRepair", self, underCursor);
		}
		
		bool CanRepair(Actor self, Actor a)
		{
			if (!a.traits.Contains<Building>()) return false;
			bool bridge = a.traits.Contains<Bridge>() && !self.Info.Traits.Get<EngineerRepairInfo>().RepairsBridges;
			
			return (bridge || self.Owner.Stances[a.Owner] == Stance.Ally);
		}

		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "EngineerRepair") return null;
			if (order.TargetActor == null) return null;
			
			return (order.TargetActor.Health == order.TargetActor.GetMaxHP()) ? "goldwrench-blocked" : "goldwrench";
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "EngineerRepair" && order.TargetActor.Health < order.TargetActor.GetMaxHP())
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				self.QueueActivity(new RepairBuilding(order.TargetActor));
			}
		}
	}
}
