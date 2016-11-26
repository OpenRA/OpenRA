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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Sets a custom terrain type for cells that are obscured by back-facing cliffs.",
		"This trait replicates the default CliffBackImpassability=2 behaviour from the TS/RA2 rules.ini.")]
	class CliffBackImpassabilityLayerInfo : ITraitInfo
	{
		public readonly string TerrainType = "Impassable";

		public object Create(ActorInitializer init) { return new CliffBackBlockingLayer(this); }
	}

	class CliffBackBlockingLayer : IWorldLoaded
	{
		readonly CliffBackImpassabilityLayerInfo info;

		public CliffBackBlockingLayer(CliffBackImpassabilityLayerInfo info)
		{
			this.info = info;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var tileType = w.Map.Rules.TileSet.GetTerrainIndex(info.TerrainType);
			foreach (var uv in w.Map.AllCells.MapCoords)
			{
				// All the map cells that visually overlap the current cell
				var testCells = w.Map.ProjectedCellsCovering(uv)
					.SelectMany(puv => w.Map.Unproject(puv));
				if (testCells.Any(x => x.V >= uv.V + 4))
					w.Map.CustomTerrain[uv] = tileType;
			}
		}
	}
}
