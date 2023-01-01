#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class GrantConditionOnTerrainInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Terrain names to trigger the condition.")]
		public readonly string[] TerrainTypes = Array.Empty<string>();

		public override object Create(ActorInitializer init) { return new GrantConditionOnTerrain(init, this); }
	}

	public class GrantConditionOnTerrain : ITick
	{
		readonly GrantConditionOnTerrainInfo info;
		readonly TerrainTypeInfo[] terrainTypes;

		int conditionToken = Actor.InvalidConditionToken;
		string cachedTerrain;

		public GrantConditionOnTerrain(ActorInitializer init, GrantConditionOnTerrainInfo info)
		{
			this.info = info;
			terrainTypes = init.World.Map.Rules.TerrainInfo.TerrainTypes;
		}

		void ITick.Tick(Actor self)
		{
			var cell = self.Location;
			if (!self.World.Map.Contains(cell))
				return;

			// The terrain type may change between ticks without the actor moving
			var currentTerrain = cell.Layer == 0 ? self.World.Map.GetTerrainInfo(cell).Type :
				terrainTypes[self.World.GetCustomMovementLayers()[cell.Layer].GetTerrainIndex(cell)].Type;

			var wantsGranted = info.TerrainTypes.Contains(currentTerrain);
			if (currentTerrain != cachedTerrain)
			{
				if (wantsGranted && conditionToken == Actor.InvalidConditionToken)
					conditionToken = self.GrantCondition(info.Condition);
				else if (!wantsGranted && conditionToken != Actor.InvalidConditionToken)
					conditionToken = self.RevokeCondition(conditionToken);
			}

			cachedTerrain = currentTerrain;
		}
	}
}
