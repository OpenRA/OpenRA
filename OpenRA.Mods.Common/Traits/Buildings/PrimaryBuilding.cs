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
	public class PrimaryBuildingInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to self while this is the primary building.")]
		public readonly string PrimaryCondition = null;

		[Desc("The speech notification to play when selecting a primary building.")]
		public readonly string SelectionNotification = "PrimaryBuildingSelected";

		public object Create(ActorInitializer init) { return new PrimaryBuilding(init.Self, this); }
	}

	public class PrimaryBuilding : INotifyCreated, IIssueOrder, IResolveOrder
	{
		readonly PrimaryBuildingInfo info;
		ConditionManager conditionManager;
		int primaryToken = ConditionManager.InvalidConditionToken;

		public bool IsPrimary { get; private set; }

		public PrimaryBuilding(Actor self, PrimaryBuildingInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get { yield return new DeployOrderTargeter("PrimaryProducer", 1); }
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "PrimaryProducer")
				return new Order(order.OrderID, self, false);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PrimaryProducer")
				SetPrimaryProducer(self, !IsPrimary);
		}

		public void SetPrimaryProducer(Actor self, bool isPrimary)
		{
			IsPrimary = isPrimary;

			if (isPrimary)
			{
				// Cancel existing primaries
				// TODO: THIS IS SHIT
				foreach (var p in self.Info.TraitInfo<ProductionInfo>().Produces)
				{
					foreach (var b in self.World
							.ActorsWithTrait<PrimaryBuilding>()
							.Where(a =>
								a.Actor != self &&
								a.Actor.Owner == self.Owner &&
								a.Trait.IsPrimary &&
								a.Actor.Info.TraitInfo<ProductionInfo>().Produces.Contains(p)))
						b.Trait.SetPrimaryProducer(b.Actor, false);
				}

				if (conditionManager != null && primaryToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(info.PrimaryCondition))
					primaryToken = conditionManager.GrantCondition(self, info.PrimaryCondition);

				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.SelectionNotification, self.Owner.Faction.InternalName);
			}
			else if (primaryToken != ConditionManager.InvalidConditionToken)
				primaryToken = conditionManager.RevokeCondition(self, primaryToken);
		}
	}
}
