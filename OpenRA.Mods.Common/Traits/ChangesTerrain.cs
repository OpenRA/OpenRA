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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the terrain type underneath the actors location.")]
	class ChangesTerrainInfo : ITraitInfo, Requires<ImmobileInfo>
	{
		[FieldLoader.Require] public readonly string TerrainType = null;

		public object Create(ActorInitializer init) { return new ChangesTerrain(this); }
	}

	class ChangesTerrain : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly ChangesTerrainInfo info;
		byte previousTerrain;

		public ChangesTerrain(ChangesTerrainInfo info)
		{
			this.info = info;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var cell = self.Location;
			var map = self.World.Map;
			var terrain = map.Rules.TileSet.GetTerrainIndex(info.TerrainType);
			previousTerrain = map.CustomTerrain[cell];
			map.CustomTerrain[cell] = terrain;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			var cell = self.Location;
			var map = self.World.Map;
			map.CustomTerrain[cell] = previousTerrain;
		}
	}
}
