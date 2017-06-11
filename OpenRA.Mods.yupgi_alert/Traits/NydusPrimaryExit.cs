#region Copyright & License Information
/*
 * Modded by Boolbada of OP mod, from Primary production faciility designation logic.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/* Works without base engine modification */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	static class NydusPrimaryExitExts
	{
		public static bool IsPrimaryNydusExit(this Actor a)
		{
			var pb = a.TraitOrDefault<NydusPrimaryExit>();
			return pb != null && pb.IsPrimary;
		}
	}

	[Desc("Used with Nydus trait for primary exit designation")]
	public class NydusPrimaryExitInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to self while this is the primary building.")]
		public readonly string PrimaryCondition = "primary";

		[Desc("The speech notification to play when selecting a primary exit.")]
		public readonly string SelectionNotification = "PrimaryBuildingSelected";

		public object Create(ActorInitializer init) { return new NydusPrimaryExit(init.Self, this); }
	}

	public class NydusPrimaryExit : IIssueOrder, IResolveOrder, INotifyCreated
	{
		readonly NydusPrimaryExitInfo info;
		ConditionManager conditionManager;
		int primaryToken = ConditionManager.InvalidConditionToken;

		public bool IsPrimary { get; private set; }

		public NydusPrimaryExit(Actor self, NydusPrimaryExitInfo info)
		{
			this.info = info;
			IsPrimary = false;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("PrimaryExit", 1); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "PrimaryExit")
				return new Order(order.OrderID, self, false);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			// You can NEVER unselect a primary nydus building, unlike primary productions buildings in RA1.
			if (order.OrderString == "PrimaryExit")
				SetPrimary(self);
		}

		public void RevokePrimary(Actor self)
		{
			IsPrimary = false;
			if (primaryToken != ConditionManager.InvalidConditionToken)
				primaryToken = conditionManager.RevokeCondition(self, primaryToken);
		}

		public void SetPrimary(Actor self)
		{
			// revoke primary of previous primary actor.
			var counter = self.Owner.PlayerActor.Trait<NydusCounter>();
			var pri = counter.PrimaryActor;

			if (pri != null)
				pri.Trait<NydusPrimaryExit>().RevokePrimary(pri);

			IsPrimary = true;
			counter.PrimaryActor = self; // keep track of primary.
			primaryToken = conditionManager.GrantCondition(self, info.PrimaryCondition);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.SelectionNotification, self.Owner.Faction.InternalName);
		}
	}
}
