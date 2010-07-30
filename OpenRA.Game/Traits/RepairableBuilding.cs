#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Traits.Activities;

namespace OpenRA.Traits
{
	public class RepairableBuildingInfo : ITraitInfo, ITraitPrerequisite<HealthInfo>
	{
		public readonly float RepairPercent = 0.2f;
		public readonly float RepairRate = 0.016f;
		public readonly int RepairStep = 7;
		public object Create(ActorInitializer init) { return new RepairableBuilding(init.self, this); }
	}

	public class RepairableBuilding : ITick, IResolveOrder
	{
		[Sync]
		bool isRepairing = false;
		
		Health Health;
		RepairableBuildingInfo Info;
		public RepairableBuilding(Actor self, RepairableBuildingInfo info)
		{
			Health = self.traits.Get<Health>();
			Info = info;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Repair")
			{
				isRepairing = !isRepairing;
			}
		}

		int remainingTicks;
		public void Tick(Actor self)
		{
			if (!isRepairing) return;

			if (remainingTicks == 0)
			{
				var csv = self.Info.Traits.GetOrDefault<CustomSellValueInfo>();
				var buildingValue = csv != null ? csv.Value : self.Info.Traits.Get<ValuedInfo>().Cost;
				
				var costPerHp = (Info.RepairPercent * buildingValue) / Health.MaxHP;
				var hpToRepair = Math.Min(Info.RepairStep, Health.MaxHP - Health.HP);
				var cost = (int)Math.Ceiling(costPerHp * hpToRepair);
				if (!self.Owner.PlayerActor.traits.Get<PlayerResources>().TakeCash(cost))
				{
					remainingTicks = 1;
					return;
				}

				self.World.AddFrameEndTask(w => w.Add(new RepairIndicator(self)));
				self.InflictDamage(self, -hpToRepair, null);
				if (Health.DamageState == DamageState.Undamaged)
				{
					isRepairing = false;
					return;
				}
				remainingTicks = (int)(Info.RepairRate * 60 * 25);
			}
			else
				--remainingTicks;
		}
	}
	public class AllowsBuildingRepairInfo : TraitInfo<AllowsBuildingRepair> {}
	public class AllowsBuildingRepair {}

}
