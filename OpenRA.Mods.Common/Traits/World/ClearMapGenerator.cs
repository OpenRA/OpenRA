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
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("A map generator that clears a map.")]
	public sealed class ClearMapGeneratorInfo : TraitInfo, IMapGeneratorInfo
	{
		[FieldLoader.Require]
		[Desc("Human-readable name this generator uses.")]
		[FluentReference]
		public readonly string Name = null;

		// This is purely of interest to the linter.
		[FieldLoader.LoadUsing(nameof(FluentReferencesLoader))]
		[FluentReference]
		public readonly List<string> FluentReferences = null;

		[FieldLoader.Require]
		[Desc("Internal id for this map generator.")]
		public readonly string Type = null;

		[FieldLoader.LoadUsing(nameof(SettingsLoader))]
		public readonly MiniYaml Settings;

		string IMapGeneratorInfo.Type => Type;

		string IMapGeneratorInfo.Name => Name;

		public override object Create(ActorInitializer init) { return new ClearMapGenerator(this); }

		static MiniYaml SettingsLoader(MiniYaml my)
		{
			return my.NodeWithKey("Settings").Value;
		}

		static List<string> FluentReferencesLoader(MiniYaml my)
		{
			return MapGeneratorSettings.DumpFluent(my.NodeWithKey("Settings").Value);
		}
	}

	public sealed class ClearMapGenerator : IMapGenerator
	{
		readonly ClearMapGeneratorInfo info;

		IMapGeneratorInfo IMapGenerator.Info => info;

		public ClearMapGenerator(ClearMapGeneratorInfo info)
		{
			this.info = info;
		}

		public MapGeneratorSettings GetSettings(Map map)
		{
			return MapGeneratorSettings.LoadSettings(info.Settings, map);
		}

		public void Generate(Map map, MiniYaml settings)
		{
			var random = new MersenneTwister();

			var tileset = map.Rules.TerrainInfo;

			if (!Exts.TryParseUshortInvariant(settings.NodeWithKey("Tile").Value.Value, out var tileType))
				throw new YamlException("Illegal tile type");

			var tile = new TerrainTile(tileType, 0);
			if (!tileset.TryGetTerrainInfo(tile, out var _))
				throw new MapGenerationException("Illegal tile type");

			// If the default terrain tile is part of a PickAny template, pick
			// a random tile index. Otherwise, just use the default tile.
			Func<TerrainTile> tilePicker;
			if (map.Rules.TerrainInfo is ITemplatedTerrainInfo templatedTerrainInfo &&
				templatedTerrainInfo.Templates.TryGetValue(tileType, out var template) &&
				template.PickAny)
			{
				tilePicker = () => new TerrainTile(tileType, (byte)random.Next(0, template.TilesCount));
			}
			else
			{
				tilePicker = () => tile;
			}

			foreach (var cell in map.AllCells)
			{
				var mpos = cell.ToMPos(map);
				map.Tiles[mpos] = tilePicker();
				map.Resources[mpos] = new ResourceTile(0, 0);
				map.Height[mpos] = 0;
			}

			map.PlayerDefinitions = new MapPlayers(map.Rules, 0).ToMiniYaml();
			map.ActorDefinitions = ImmutableArray<MiniYamlNode>.Empty;
		}
	}
}
