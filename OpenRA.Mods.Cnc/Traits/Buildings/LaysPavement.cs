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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Lays isometric terrain whose sprite is picked by it's neighbours.")]
	public class LaysPavementInfo : TraitInfo, Requires<BuildingInfo>
	{
		[FieldLoader.Require]
		[Desc("The terrain types that this template will be placed on.")]
		public readonly HashSet<string> TerrainTypes = new();

		[Desc("Offset relative to the actor TopLeft. Not used if the template is PickAny.",
			"Tiles being offset out of the actor's footprint will not be placed.")]
		public readonly CVec Offset = CVec.Zero;

		public override object Create(ActorInitializer init) { return new LaysPavement(init.Self, this); }
	}

	public class LaysPavement : INotifyAddedToWorld
	{
		readonly LaysPavementInfo info;
		readonly PavementLayer layer;
		readonly BuildingInfluence buildingInfluence;
		readonly BuildingInfo buildingInfo;
		readonly Map map;

		public LaysPavement(Actor self, LaysPavementInfo info)
		{
			map = self.World.Map;
			this.info = info;
			layer = self.World.WorldActor.Trait<PavementLayer>();
			buildingInfluence = self.World.WorldActor.Trait<BuildingInfluence>();
			buildingInfo = self.Info.TraitInfo<BuildingInfo>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			foreach (var cell in buildingInfo.Tiles(self.Location))
			{
				if (!map.Contains(cell))
					continue;

				// Only place on allowed terrain types
				if (!info.TerrainTypes.Contains(map.GetTerrainInfo(cell).Type))
					continue;

				// Can never be placed on ramps
				if (map.Ramp[cell] != 0)
					continue;

				// Don't place under other buildings
				if (buildingInfluence.GetBuildingsAt(cell).Any(a => a.Info.Name != self.Info.Name))
					continue;

				// or custom terrain like resources
				if (map.CustomTerrain[cell] != byte.MaxValue)
					continue;

				layer.Add(cell);
			}
		}
	}
}
