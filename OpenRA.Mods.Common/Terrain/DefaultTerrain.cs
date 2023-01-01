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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.Common.Terrain
{
	public class DefaultTerrainLoader : ITerrainLoader
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "IDE0060:Remove unused parameter", Justification = "Load game API")]
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
		public readonly string[] Images;
		public readonly string[] DepthImages;
		public readonly int[] Frames;
		public readonly string Palette;

		public DefaultTerrainTemplateInfo(ITerrainInfo terrainInfo, MiniYaml my)
			: base(terrainInfo, my) { }

		protected override TerrainTileInfo LoadTileInfo(ITerrainInfo terrainInfo, MiniYaml my)
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

	public class DefaultTerrain : ITemplatedTerrainInfo, ITerrainInfoNotifyMapCreated
	{
		public readonly string Name;
		public readonly string Id;
		public readonly int SheetSize = 512;
		public readonly Color[] HeightDebugColors = { Color.Red };
		public readonly string[] EditorTemplateOrder;
		public readonly bool IgnoreTileSpriteOffsets;
		public readonly bool EnableDepth = false;
		public readonly float MinHeightColorBrightness = 1.0f;
		public readonly float MaxHeightColorBrightness = 1.0f;
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[FieldLoader.Ignore]
		public readonly IReadOnlyDictionary<ushort, TerrainTemplateInfo> Templates;

		[FieldLoader.Ignore]
		public readonly TerrainTypeInfo[] TerrainInfo;
		readonly Dictionary<string, byte> terrainIndexByType = new Dictionary<string, byte>();
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
				.ToArray();

			if (TerrainInfo.Length >= byte.MaxValue)
				throw new YamlException("Too many terrain types.");

			for (byte i = 0; i < TerrainInfo.Length; i++)
			{
				var tt = TerrainInfo[i].Type;

				if (terrainIndexByType.ContainsKey(tt))
					throw new YamlException($"Duplicate terrain type '{tt}' in '{filepath}'.");

				terrainIndexByType.Add(tt, i);
			}

			defaultWalkableTerrainIndex = GetTerrainIndex("Clear");

			// Templates
			Templates = yaml["Templates"].ToDictionary().Values
				.Select(y => (TerrainTemplateInfo)new DefaultTerrainTemplateInfo(this, y)).ToDictionary(t => t.Id);
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
		TerrainTypeInfo[] ITerrainInfo.TerrainTypes => TerrainInfo;
		TerrainTileInfo ITerrainInfo.GetTerrainInfo(TerrainTile r) { return GetTileInfo(r); }
		bool ITerrainInfo.TryGetTerrainInfo(TerrainTile r, out TerrainTileInfo info) { return TryGetTileInfo(r, out info); }
		Color[] ITerrainInfo.HeightDebugColors => HeightDebugColors;
		IEnumerable<Color> ITerrainInfo.RestrictedPlayerColors { get { return TerrainInfo.Where(ti => ti.RestrictPlayerColor).Select(ti => ti.Color); } }
		float ITerrainInfo.MinHeightColorBrightness => MinHeightColorBrightness;
		float ITerrainInfo.MaxHeightColorBrightness => MaxHeightColorBrightness;
		TerrainTile ITerrainInfo.DefaultTerrainTile => new TerrainTile(Templates.First().Key, 0);

		string[] ITemplatedTerrainInfo.EditorTemplateOrder => EditorTemplateOrder;
		IReadOnlyDictionary<ushort, TerrainTemplateInfo> ITemplatedTerrainInfo.Templates => Templates;

		void ITerrainInfoNotifyMapCreated.MapCreated(Map map)
		{
			// Randomize PickAny tile variants
			var r = new MersenneTwister();
			for (var j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
			{
				for (var i = map.Bounds.Left; i < map.Bounds.Right; i++)
				{
					var type = map.Tiles[new MPos(i, j)].Type;
					if (!Templates.TryGetValue(type, out var template) || !template.PickAny)
						continue;

					map.Tiles[new MPos(i, j)] = new TerrainTile(type, (byte)r.Next(0, template.TilesCount));
				}
			}
		}
	}
}
