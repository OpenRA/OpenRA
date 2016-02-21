#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	static class PrimaryExts
	{
		public static bool IsPrimaryBuilding(this Actor a)
		{
			var pb = a.TraitOrDefault<PrimaryBuilding>();
			return pb != null && pb.IsPrimary;
		}
	}

	[Desc("Used together with ClassicProductionQueue.")]
	public class PrimaryBuildingInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[UpgradeGrantedReference, Desc("The upgrades to grant while the primary building.")]
		public readonly string[] Upgrades = { "primary" };

		[Desc("The speech notification to play when selecting a primary building.")]
		public readonly string SelectionNotification = "PrimaryBuildingSelected";

		public object Create(ActorInitializer init) { return new PrimaryBuilding(init.Self, this); }
	}

	public class PrimaryBuilding : IIssueOrder, IResolveOrder
	{
		readonly PrimaryBuildingInfo info;
		readonly UpgradeManager manager;

		public bool IsPrimary { get; private set; }

		public PrimaryBuilding(Actor self, PrimaryBuildingInfo info)
		{
			this.info = info;
			manager = self.Trait<UpgradeManager>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("PrimaryProducer", 1); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "PrimaryProducer")
				return new Order(order.OrderID, self, false);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PrimaryProducer")
				SetPrimaryProducer(self, !IsPrimary);
		}

		public void SetPrimaryProducer(Actor self, bool state)
		{
			if (state == false)
			{
				IsPrimary = false;
				foreach (var up in info.Upgrades)
					manager.RevokeUpgrade(self, up, this);
				return;
			}

			// TODO: THIS IS SHIT
			// Cancel existing primaries
			foreach (var p in self.Info.TraitInfo<ProductionInfo>().Produces)
			{
				var productionType = p;		// benign closure hazard
				foreach (var b in self.World
					.ActorsWithTrait<PrimaryBuilding>()
					.Where(a =>
						a.Actor.Owner == self.Owner &&
						a.Trait.IsPrimary &&
						a.Actor.Info.TraitInfo<ProductionInfo>().Produces.Contains(productionType)))
					b.Trait.SetPrimaryProducer(b.Actor, false);
			}

			IsPrimary = true;
			foreach (var up in info.Upgrades)
				manager.GrantUpgrade(self, up, this);

			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.SelectionNotification, self.Owner.Faction.InternalName);
		}
	}
}
