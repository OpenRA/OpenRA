#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the player actor to allow building repair by team mates.")]
	class AllyRepairInfo : TraitInfo<AllyRepair> { }

	class AllyRepair : IResolveOrder
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "RepairBuilding" && order.Target.Type == TargetType.Actor)
			{
				var building = order.Target.Actor;
				if (!building.AppearsFriendlyTo(self))
					return;

				var rb = building.TraitOrDefault<RepairableBuilding>();
				if (rb != null)
					rb.RepairBuilding(building, self.Owner);
			}
		}
	}
}
