#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class DeployToUpgradeInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[Desc("The upgrades to grant when deploying and revoke when undeploying.")]
		public readonly string[] Upgrades = { };

		[Desc("Cursor to display when deploying the actor.")]
		public readonly string Cursor = "deploy";

		public object Create(ActorInitializer init) { return new DeployToUpgrade(init.Self, this); }
	}

	public class DeployToUpgrade : IResolveOrder, IIssueOrder
	{
		readonly DeployToUpgradeInfo info;
		readonly UpgradeManager manager;

		bool isUpgraded;

		public DeployToUpgrade(Actor self, DeployToUpgradeInfo info)
		{
			this.info = info;
			manager = self.Trait<UpgradeManager>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("DeployToUpgrade", 5, info.Cursor); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "DeployToUpgrade")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeployToUpgrade")
				return;

			if (isUpgraded)
				foreach (var up in info.Upgrades)
					manager.RevokeUpgrade(self, up, this);
			else
				foreach (var up in info.Upgrades)
					manager.GrantUpgrade(self, up, this);

			isUpgraded = !isUpgraded;
		}
	}
}