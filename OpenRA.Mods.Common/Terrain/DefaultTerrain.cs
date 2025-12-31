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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.UtilityCommands;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.Common.Terrain
{
	public class DefaultTerrainLoader : ITerrainLoader
	{
		public DefaultTerrainLoader(ModData modData) { }

		public ITerrainInfo ParseTerrain(IReadOnlyFileSystem fileSystem, string path)
		{
			return new DefaultTerrain(fileSystem, path);
		}
	}

	public class DefaultTerrainTileInfo : TerrainTileInfo
	{
		public readonly float ZOffset = 0.0f;
		public readonly float ZRamp = 1.0f;
	}

	public class DefaultTerrainTemplateInfo : TerrainTemplateInfo
	{
		public readonly ImmutableArray<string> Images;
		public readonly ImmutableArray<string> DepthImages;
		public readonly ImmutableArray<int> Frames;
		public readonly string Palette;

		public DefaultTerrainTemplateInfo(ITerrainInfo terrainInfo, MiniYaml my)
			: base(terrainInfo, my) { }

		protected override DefaultTerrainTileInfo LoadTileInfo(ITerrainInfo terrainInfo, MiniYaml my)
		{
			var tile = new DefaultTerrainTileInfo();
			FieldLoader.Load(tile, my);

			// Terrain type must be converted from a string to an index
			tile.GetType().GetField(nameof(tile.TerrainType)).SetValue(tile, terrainInfo.GetTerrainIndex(my.Value));

			// Fall back to the terrain-type color if necessary
			var overrideColor = terrainInfo.TerrainTypes[tile.TerrainType].Color;
			if (tile.MinColor == default)
				tile.GetType().GetField(nameof(tile.MinColor)).SetValue(tile, overrideColor);

			if (tile.MaxColor == default)
				tile.GetType().GetField(nameof(tile.MaxColor)).SetValue(tile, overrideColor);

			return tile;
		}
	}

	public class DefaultTerrain : ITemplatedTerrainInfo, IDumpSheetsTerrainInfo, ITerrainInfoNotifyMapCreated
	{
		[FluentReference]
		public readonly string Name;
		public readonly string Id;
		public readonly Size TileSize = new(24, 24);
		public readonly int SheetSize = 512;
		public readonly ImmutableArray<Color> HeightDebugColors = [Color.Red];
		public readonly ImmutableArray<string> EditorTemplateOrder;
		public readonly bool IgnoreTileSpriteOffsets;
		public readonly bool EnableDepth = false;
		public readonly float MinHeightColorBrightness = 1.0f;
		public readonly float MaxHeightColorBrightness = 1.0f;
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[FieldLoader.Ignore]
		public readonly FrozenDictionary<ushort, TerrainTemplateInfo> Templates;
		[FieldLoader.Ignore]
		public readonly ImmutableArray<TerrainTemplateInfo> TemplatesInDefinitionOrder;
		[FieldLoader.Ignore]
		public readonly FrozenDictionary<string, ImmutableArray<MultiBrushInfo>> MultiBrushCollections;

		[FieldLoader.Ignore]
		public readonly ImmutableArray<TerrainTypeInfo> TerrainInfo;
		readonly FrozenDictionary<string, byte> terrainIndexByType;
		readonly byte defaultWalkableTerrainIndex;

		public DefaultTerrain(IReadOnlyFileSystem fileSystem, string filepath)
		{
			var yaml = MiniYaml.FromStream(fileSystem.Open(filepath), filepath)
				.ToDictionary(x => x.Key, x => x.Value);

			// General info
			FieldLoader.Load(this, yaml["General"]);

			// TerrainTypes
			TerrainInfo = yaml["Terrain"].ToDictionary().Values
				.Select(y => new TerrainTypeInfo(y))
				.OrderBy(tt => tt.Type)
				.ToImmutableArray();

			if (TerrainInfo.Length >= byte.MaxValue)
				throw new YamlException("Too many terrain types.");

			var tiby = new Dictionary<string, byte>(TerrainInfo.Length);
			for (byte i = 0; i < TerrainInfo.Length; i++)
			{
				var tt = TerrainInfo[i].Type;

				if (!tiby.TryAdd(tt, i))
					throw new YamlException($"Duplicate terrain type '{tt}' in '{filepath}'.");
			}

			terrainIndexByType = tiby.ToFrozenDictionary();

			defaultWalkableTerrainIndex = GetTerrainIndex("Clear");

			// Templates
			TemplatesInDefinitionOrder = yaml["Templates"].Nodes
				.Select(n => (TerrainTemplateInfo)new DefaultTerrainTemplateInfo(this, n.Value))
				.ToImmutableArray();
			Templates = TemplatesInDefinitionOrder
				.ToFrozenDictionary(t => t.Id);

			MultiBrushCollections =
				yaml.TryGetValue("MultiBrushCollections", out var collectionDefinitions)
					? collectionDefinitions.ToDictionary()
						.Select(kv => new KeyValuePair<string, ImmutableArray<MultiBrushInfo>>(
							kv.Key,
							MultiBrushInfo.ParseCollection(kv.Value)))
						.ToFrozenDictionary()
					: FrozenDictionary<string, ImmutableArray<MultiBrushInfo>>.Empty;
		}

		public TerrainTypeInfo this[byte index] => TerrainInfo[index];

		public byte GetTerrainIndex(string type)
		{
			if (terrainIndexByType.TryGetValue(type, out var index))
				return index;

			throw new InvalidDataException($"Tileset '{Id}' lacks terrain type '{type}'");
		}

		public byte GetTerrainIndex(TerrainTile r)
		{
			var tile = Templates[r.Type][r.Index];
			if (tile.TerrainType != byte.MaxValue)
				return tile.TerrainType;

			return defaultWalkableTerrainIndex;
		}

		public TerrainTileInfo GetTileInfo(TerrainTile r)
		{
			return Templates[r.Type][r.Index];
		}

		public bool TryGetTileInfo(TerrainTile r, out TerrainTileInfo info)
		{
			if (!Templates.TryGetValue(r.Type, out var tpl) || !tpl.Contains(r.Index))
			{
				info = null;
				return false;
			}

			info = tpl[r.Index];
			return info != null;
		}

		string ITerrainInfo.Id => Id;
		string ITerrainInfo.Name => Name;
		Size ITerrainInfo.TileSize => TileSize;
		ImmutableArray<TerrainTypeInfo> ITerrainInfo.TerrainTypes => TerrainInfo;
		TerrainTileInfo ITerrainInfo.GetTerrainInfo(TerrainTile r) { return GetTileInfo(r); }
		bool ITerrainInfo.TryGetTerrainInfo(TerrainTile r, out TerrainTileInfo info) { return TryGetTileInfo(r, out info); }
		ImmutableArray<Color> ITerrainInfo.HeightDebugColors => HeightDebugColors;
		IEnumerable<Color> ITerrainInfo.RestrictedPlayerColors { get { return TerrainInfo.Where(ti => ti.RestrictPlayerColor).Select(ti => ti.Color); } }
		float ITerrainInfo.MinHeightColorBrightness => MinHeightColorBrightness;
		float ITerrainInfo.MaxHeightColorBrightness => MaxHeightColorBrightness;

		TerrainTile ITerrainInfo.DefaultTerrainTile => new(TemplatesInDefinitionOrder[0].Id, 0);

		ImmutableArray<string> ITemplatedTerrainInfo.EditorTemplateOrder => EditorTemplateOrder;
		FrozenDictionary<ushort, TerrainTemplateInfo> ITemplatedTerrainInfo.Templates => Templates;
		ImmutableArray<TerrainTemplateInfo> ITemplatedTerrainInfo.TemplatesInDefinitionOrder => TemplatesInDefinitionOrder;
		FrozenDictionary<string, ImmutableArray<MultiBrushInfo>> ITemplatedTerrainInfo.MultiBrushCollections => MultiBrushCollections;

		void IDumpSheetsTerrainInfo.DumpSheets(string terrainName, ImmutablePalette palette, ref int sheetCount)
		{
			var tileCache = new DefaultTileCache(this);
			var sb = tileCache.GetSheetBuilder(SheetType.Indexed);
			foreach (var s in sb.AllSheets)
				DumpSequenceSheetsCommand.CommitSheet(sb, s, terrainName, palette, ref sheetCount);

			foreach (var s in tileCache.GetSheetBuilder(SheetType.BGRA).AllSheets)
				DumpSequenceSheetsCommand.CommitSheet(null, s, terrainName, palette, ref sheetCount);
		}

		void ITerrainInfoNotifyMapCreated.MapCreated(Map map)
		{
			// Randomize PickAny tile variants.
			var r = new MersenneTwister();
			foreach (var uv in map.AllCells.MapCoords)
			{
				var type = map.Tiles[uv].Type;
				if (!Templates.TryGetValue(type, out var template) || !template.PickAny)
					continue;

				map.Tiles[uv] = new TerrainTile(type, (byte)r.Next(0, template.TilesCount));
			}
		}
	}
}
