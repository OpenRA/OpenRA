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

using System.Collections.Frozen;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the terrain type underneath the actor's location.",
		"Make sure that the actor doesn't move, as the terrain is changed only on actor creation.",
		"In other words using Mobile, Aircraft nor any other IMove-based trait is supported " +
		"and can cause unintended side effects.")]
	sealed class ChangesTerrainInfo : TraitInfo
	{
		[FieldLoader.Require]
		public readonly string TerrainType = null;

		[Desc("Only change terrain, if the cell's original terrain type is in this list.",
			"By default, the terrain type is changed regardless of the original terrain type.")]
		public readonly FrozenSet<string> TerrainTypes = null;

		public override object Create(ActorInitializer init) { return new ChangesTerrain(this); }
	}

	sealed class ChangesTerrain : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly ChangesTerrainInfo info;
		byte? previousTerrain;

		public ChangesTerrain(ChangesTerrainInfo info)
		{
			this.info = info;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var cell = self.Location;
			var map = self.World.Map;

			if (info.TerrainTypes?.Contains(map.GetTerrainInfo(cell).Type) == false)
				return;

			var terrain = map.Rules.TerrainInfo.GetTerrainIndex(info.TerrainType);

			previousTerrain = map.CustomTerrain[cell];
			map.CustomTerrain[cell] = terrain;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (previousTerrain == null)
				return;

			var cell = self.Location;
			var map = self.World.Map;
			map.CustomTerrain[cell] = previousTerrain.Value;
		}
	}
}
