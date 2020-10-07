#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.D2k.Traits
{
	public class LaysTerrainInfo : ConditionalTraitInfo, Requires<BuildingInfo>
	{
		[Desc("The terrain template to place. If the template is PickAny, then " +
			"the actor footprint will be filled with this tile.")]
		public readonly ushort Template = 0;

		[FieldLoader.Require]
		[Desc("The terrain types that this template will be placed on.")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		[Desc("Offset relative to the actor TopLeft. Not used if the template is PickAny.",
			"Tiles being offset out of the actor's footprint will not be placed.")]
		public readonly CVec Offset = CVec.Zero;

		public override object Create(ActorInitializer init) { return new LaysTerrain(init.Self, this); }
	}

	public class LaysTerrain : ConditionalTrait<LaysTerrainInfo>, INotifyAddedToWorld
	{
		readonly BuildableTerrainLayer layer;
		readonly TerrainTemplateInfo template;
		readonly BuildingInfo buildingInfo;

		public LaysTerrain(Actor self, LaysTerrainInfo info)
			: base(info)
		{
			layer = self.World.WorldActor.Trait<BuildableTerrainLayer>();
			template = self.World.Map.Rules.TileSet.Templates[info.Template];
			buildingInfo = self.Info.TraitInfo<BuildingInfo>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var map = self.World.Map;

			if (template.PickAny)
			{
				// Fill the footprint with random variants
				foreach (var c in buildingInfo.Tiles(self.Location))
				{
					// Only place on allowed terrain types
					if (!map.Contains(c) || !Info.TerrainTypes.Contains(map.GetTerrainInfo(c).Type))
						continue;

					// Don't override any existing custom terrain
					if (map.CustomTerrain[c] != byte.MaxValue)
						continue;

					// Don't place under other buildings
					if (self.World.ActorMap.GetActorsAt(c).Any(a => a != self && a.TraitOrDefault<Building>() != null))
						continue;

					var index = Game.CosmeticRandom.Next(template.TilesCount);
					layer.AddTile(c, new TerrainTile(template.Id, (byte)index));
				}

				return;
			}

			var origin = self.Location + Info.Offset;
			for (var i = 0; i < template.TilesCount; i++)
			{
				var c = origin + new CVec(i % template.Size.X, i / template.Size.X);

				// Only place on allowed terrain types
				if (!Info.TerrainTypes.Contains(map.GetTerrainInfo(c).Type))
					continue;

				// Don't override any existing custom terrain
				if (map.CustomTerrain[c] != byte.MaxValue)
					continue;

				// Don't place under other buildings
				if (self.World.ActorMap.GetActorsAt(c).Any(a => a != self && a.TraitOrDefault<Building>() != null))
					continue;

				layer.AddTile(c, new TerrainTile(template.Id, (byte)i));
			}
		}
	}
}
