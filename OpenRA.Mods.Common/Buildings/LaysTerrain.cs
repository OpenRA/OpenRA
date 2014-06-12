#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Buildings
{
	public class LaysTerrainInfo : ITraitInfo, Requires<BuildingInfo>, Requires<RenderSpritesInfo>
	{
		[Desc("The terrain template to place. If the template is PickAny, then" +
			"the actor footprint will be filled with this tile.")]
		public readonly ushort Template = 0;

		[Desc("The terrain types that this template will be placed on")]
		public readonly string[] TerrainTypes = {};

		[Desc("Offset relative to the actor TopLeft. Not used if the template is PickAny")]
		public readonly CVec Offset = CVec.Zero;

		public object Create(ActorInitializer init) { return new LaysTerrain(init.self, this); }
	}

	public class LaysTerrain : INotifyAddedToWorld
	{
		readonly LaysTerrainInfo info;
		readonly BuildableTerrainLayer layer;
		readonly BuildingInfluence bi;
		readonly TileTemplate template;

		public LaysTerrain(Actor self, LaysTerrainInfo info)
		{
			this.info = info;
			layer = self.World.WorldActor.Trait<BuildableTerrainLayer>();
			bi = self.World.WorldActor.Trait<BuildingInfluence>();
			template = self.World.TileSet.Templates[info.Template];
		}

		public void AddedToWorld(Actor self)
		{
			if (template.PickAny)
			{
				// Fill the footprint with random variants
				foreach (var c in FootprintUtils.Tiles(self))
				{
					// Only place on allowed terrain types
					if (!info.TerrainTypes.Contains(self.World.GetTerrainType(c)))
						continue;

					// Don't place under other buildings or custom terrain
					if (bi.GetBuildingAt(c) != self || self.World.Map.CustomTerrain[c.X, c.Y] != null)
						continue;

					var index = template.Tiles.Keys.Random(Game.CosmeticRandom);
					layer.AddTile(c, new TileReference<ushort, byte>(template.Id, index));
				}

				return;
			}

			var origin = self.Location + info.Offset;
			foreach (var i in template.Tiles.Keys)
			{
				var c = origin + new CVec(i % template.Size.X, i / template.Size.X);

				// Only place on allowed terrain types
				if (!info.TerrainTypes.Contains(self.World.GetTerrainType(c)))
					continue;

				// Don't place under other buildings or custom terrain
				if (bi.GetBuildingAt(c) != self || self.World.Map.CustomTerrain[c.X, c.Y] != null)
					continue;

				layer.AddTile(c, new TileReference<ushort, byte>(template.Id, i));
			}
		}
	}
}
