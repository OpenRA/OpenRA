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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class GrantConditionOnTerrainInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Terrain names to trigger the upgrade.")]
		public readonly string[] TerrainTypes = { };

		public object Create(ActorInitializer init) { return new GrantConditionOnTerrain(init, this); }
	}

	public class GrantConditionOnTerrain : INotifyCreated, ITick
	{
		readonly GrantConditionOnTerrainInfo info;

		UpgradeManager manager;
		int conditionToken = UpgradeManager.InvalidConditionToken;
		string previousTerrain;

		public GrantConditionOnTerrain(ActorInitializer init, GrantConditionOnTerrainInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.TraitOrDefault<UpgradeManager>();
		}

		public void Tick(Actor self)
		{
			if (manager == null)
				return;

			var currentTerrain = self.World.Map.GetTerrainInfo(self.Location).Type;
			var wantsGranted = info.TerrainTypes.Contains(currentTerrain);
			if (currentTerrain != previousTerrain)
			{
				if (wantsGranted && conditionToken == UpgradeManager.InvalidConditionToken)
					conditionToken = manager.GrantCondition(self, info.Condition);
				else if (!wantsGranted && conditionToken != UpgradeManager.InvalidConditionToken)
					conditionToken = manager.RevokeCondition(self, conditionToken);
			}

			previousTerrain = currentTerrain;
		}
	}
}
