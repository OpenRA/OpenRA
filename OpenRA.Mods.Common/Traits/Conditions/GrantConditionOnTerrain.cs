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
		[Desc("Terrain names to trigger the condition.")]
		public readonly string[] TerrainTypes = { };

		public object Create(ActorInitializer init) { return new GrantConditionOnTerrain(init, this); }
	}

	public class GrantConditionOnTerrain : INotifyCreated, ITick
	{
		readonly GrantConditionOnTerrainInfo info;
		readonly TileSet tileSet;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		string cachedTerrain;
		CPos cachedLocation;

		public GrantConditionOnTerrain(ActorInitializer init, GrantConditionOnTerrainInfo info)
		{
			this.info = info;
			tileSet = init.World.Map.Rules.TileSet;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void ITick.Tick(Actor self)
		{
			var loc = self.Location;
			if (conditionManager == null || loc == cachedLocation)
				return;

			var currentTerrain = loc.Layer == 0 ? self.World.Map.GetTerrainInfo(loc).Type :
					tileSet[self.World.GetCustomMovementLayers()[loc.Layer].GetTerrainIndex(loc)].Type;

			var wantsGranted = info.TerrainTypes.Contains(currentTerrain);
			if (currentTerrain != cachedTerrain)
			{
				if (wantsGranted && conditionToken == ConditionManager.InvalidConditionToken)
					conditionToken = conditionManager.GrantCondition(self, info.Condition);
				else if (!wantsGranted && conditionToken != ConditionManager.InvalidConditionToken)
					conditionToken = conditionManager.RevokeCondition(self, conditionToken);
			}

			cachedTerrain = currentTerrain;
			cachedLocation = loc;
		}
	}
}
