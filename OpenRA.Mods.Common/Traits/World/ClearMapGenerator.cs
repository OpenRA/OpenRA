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

using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	[Desc("A map generator that clears a map.")]
	public sealed class ClearMapGeneratorInfo : MapGeneratorBaseInfo
	{
		protected override int GetPlayerCount(MapGenerationArgs args) => 0;

		public override Map Generate(ModData modData, MapGenerationArgs args)
		{
			var random = new MersenneTwister();
			if (!Exts.TryParseUInt16Invariant(GenerateParameterYaml(modData, args).NodeWithKey("Tile").Value.Value, out var tileType))
				throw new YamlException("Illegal tile type");

			var terrainInfo = modData.DefaultTerrainInfo[args.Tileset];
			if (!terrainInfo.TryGetTerrainInfo(new TerrainTile(tileType, 0), out _))
				throw new MapGenerationException("Illegal tile type");

			var map = new Map(modData, terrainInfo, args.Size);
			var terraformer = new Terraformer(args, map, modData, [], Symmetry.Mirror.None, 1);

			terraformer.InitMap();

			foreach (var mpos in map.AllCells.MapCoords)
				map.Tiles[mpos] = terraformer.PickTile(random, tileType);

			terraformer.BakeMap();

			return map;
		}

		public override object Create(ActorInitializer init)
		{
			return new ClearMapGenerator(init, this);
		}
	}

	public class ClearMapGenerator : MapGeneratorBase
	{
		public ClearMapGenerator(ActorInitializer init, ClearMapGeneratorInfo info)
			: base(init, info) { }
	}
}
