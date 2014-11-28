#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA

{
	[Desc("Attach this to the player actor to allow building repair by team mates.")]
	class AllyRepairInfo : TraitInfo<AllyRepair> { }

	class AllyRepair : IResolveOrder
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.ID == OrderCode.RepairBuilding)
			{
				var building = order.TargetActor;

				if (building.HasTrait<RepairableBuilding>())
					if (building.AppearsFriendlyTo(self))
						building.Trait<RepairableBuilding>().RepairBuilding(building, self.Owner);
			}
		}
	}
}
