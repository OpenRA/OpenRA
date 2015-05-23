﻿#region Copyright & License Information
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
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class DeployToUpgradeInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[Desc("The upgrades to grant when deploying and revoke when undeploying.")]
		public readonly string[] Upgrades = { };

		[Desc("The terrain types that this actor can deploy on to receive these upgrades." +
			"Leave empty to allow any.")]
		public readonly string[] AllowedTerrainTypes = { };

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		public object Create(ActorInitializer init) { return new DeployToUpgrade(init.Self, this); }
	}

	public class DeployToUpgrade : IResolveOrder, IIssueOrder
	{
		readonly Actor self;
		readonly DeployToUpgradeInfo info;
		readonly UpgradeManager manager;
		readonly bool checkTerrainType;

		bool isUpgraded;

		public DeployToUpgrade(Actor self, DeployToUpgradeInfo info)
		{
			this.self = self;
			this.info = info;
			manager = self.Trait<UpgradeManager>();
			checkTerrainType = info.AllowedTerrainTypes.Length > 0;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("DeployToUpgrade", 5,
				() => OnValidTerrain() ? info.DeployCursor : info.DeployBlockedCursor); }
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

			if (!OnValidTerrain())
				return;

			if (isUpgraded)
				foreach (var up in info.Upgrades)
					manager.RevokeUpgrade(self, up, this);
			else
				foreach (var up in info.Upgrades)
					manager.GrantUpgrade(self, up, this);

			isUpgraded = !isUpgraded;
		}

		bool OnValidTerrain()
		{
			if (!self.World.Map.Contains(self.Location))
				return false;

			if (!checkTerrainType)
				return true;

			var tileSet = self.World.TileSet;
			var tiles = self.World.Map.MapTiles.Value;
			var terrainType = tileSet[tileSet.GetTerrainIndex(tiles[self.Location])].Type;

			return info.AllowedTerrainTypes.Contains(terrainType);
		}
	}
}