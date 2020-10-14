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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public interface ITerrainInfo
	{
		string Id { get; }
		TerrainTypeInfo[] TerrainTypes { get; }
		TerrainTileInfo GetTerrainInfo(TerrainTile r);
		bool TryGetTerrainInfo(TerrainTile r, out TerrainTileInfo info);
		byte GetTerrainIndex(string type);
		byte GetTerrainIndex(TerrainTile r);
		TerrainTile DefaultTerrainTile { get; }

		Color[] HeightDebugColors { get; }
		IEnumerable<Color> RestrictedPlayerColors { get; }
		float MinHeightColorBrightness { get; }
		float MaxHeightColorBrightness { get; }
	}

	public interface ITemplatedTerrainInfo : ITerrainInfo
	{
		string[] EditorTemplateOrder { get; }
		IReadOnlyDictionary<ushort, TerrainTemplateInfo> Templates { get; }
	}

	public interface ITerrainInfoNotifyMapCreated : ITerrainInfo
	{
		void MapCreated(Map map);
	}

	public class TerrainTileInfo
	{
		[FieldLoader.Ignore]
		public readonly byte TerrainType = byte.MaxValue;
		public readonly byte Height;
		public readonly byte RampType;
		public readonly Color MinColor;
		public readonly Color MaxColor;
		public readonly float ZOffset = 0.0f;
		public readonly float ZRamp = 1.0f;
	}

	public class TerrainTypeInfo
	{
		public readonly string Type;
		public readonly BitSet<TargetableType> TargetTypes;
		public readonly HashSet<string> AcceptsSmudgeType = new HashSet<string>();
		public readonly Color Color;
		public readonly bool RestrictPlayerColor = false;
		public readonly string CustomCursor;

		public TerrainTypeInfo(MiniYaml my) { FieldLoader.Load(this, my); }
	}

	public class TerrainTemplateInfo
	{
		public readonly ushort Id;
		public readonly string[] Images;
		public readonly int[] Frames;
		public readonly int2 Size;
		public readonly bool PickAny;
		public readonly string[] Categories;
		public readonly string Palette;

		readonly TerrainTileInfo[] tileInfo;

		public TerrainTemplateInfo(ITerrainInfo terrainInfo, MiniYaml my)
		{
			FieldLoader.Load(this, my);

			var nodes = my.ToDictionary()["Tiles"].Nodes;

			if (!PickAny)
			{
				tileInfo = new TerrainTileInfo[Size.X * Size.Y];
				foreach (var node in nodes)
				{
					if (!int.TryParse(node.Key, out var key))
						throw new YamlException("Tileset `{0}` template `{1}` defines a frame `{2}` that is not a valid integer.".F(terrainInfo.Id, Id, node.Key));

					if (key < 0 || key >= tileInfo.Length)
						throw new YamlException("Tileset `{0}` template `{1}` references frame {2}, but only [0..{3}] are valid for a {4}x{5} Size template.".F(terrainInfo.Id, Id, key, tileInfo.Length - 1, Size.X, Size.Y));

					tileInfo[key] = LoadTileInfo(terrainInfo, node.Value);
				}
			}
			else
			{
				tileInfo = new TerrainTileInfo[nodes.Count];

				var i = 0;
				foreach (var node in nodes)
				{
					if (!int.TryParse(node.Key, out var key))
						throw new YamlException("Tileset `{0}` template `{1}` defines a frame `{2}` that is not a valid integer.".F(terrainInfo.Id, Id, node.Key));

					if (key != i++)
						throw new YamlException("Tileset `{0}` template `{1}` is missing a definition for frame {2}.".F(terrainInfo.Id, Id, i - 1));

					tileInfo[key] = LoadTileInfo(terrainInfo, node.Value);
				}
			}
		}

		static TerrainTileInfo LoadTileInfo(ITerrainInfo terrainInfo, MiniYaml my)
		{
			var tile = new TerrainTileInfo();
			FieldLoader.Load(tile, my);

			// Terrain type must be converted from a string to an index
			tile.GetType().GetField("TerrainType").SetValue(tile, terrainInfo.GetTerrainIndex(my.Value));

			// Fall back to the terrain-type color if necessary
			var overrideColor = terrainInfo.TerrainTypes[tile.TerrainType].Color;
			if (tile.MinColor == default)
				tile.GetType().GetField("MinColor").SetValue(tile, overrideColor);

			if (tile.MaxColor == default)
				tile.GetType().GetField("MaxColor").SetValue(tile, overrideColor);

			return tile;
		}

		public TerrainTileInfo this[int index] { get { return tileInfo[index]; } }

		public bool Contains(int index)
		{
			return index >= 0 && index < tileInfo.Length;
		}

		public int TilesCount
		{
			get { return tileInfo.Length; }
		}
	}

	public class TileSet : ITemplatedTerrainInfo, ITerrainInfoNotifyMapCreated
	{
		public const string TerrainPaletteInternalName = "terrain";

		public readonly string Name;
		public readonly string Id;
		public readonly int SheetSize = 512;
		public readonly Color[] HeightDebugColors = new[] { Color.Red };
		public readonly string[] EditorTemplateOrder;
		public readonly bool IgnoreTileSpriteOffsets;
		public readonly bool EnableDepth = false;
		public readonly float MinHeightColorBrightness = 1.0f;
		public readonly float MaxHeightColorBrightness = 1.0f;

		[FieldLoader.Ignore]
		public readonly IReadOnlyDictionary<ushort, TerrainTemplateInfo> Templates;

		[FieldLoader.Ignore]
		public readonly TerrainTypeInfo[] TerrainInfo;
		readonly Dictionary<string, byte> terrainIndexByType = new Dictionary<string, byte>();
		readonly byte defaultWalkableTerrainIndex;

		public TileSet(IReadOnlyFileSystem fileSystem, string filepath)
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
					throw new YamlException("Duplicate terrain type '{0}' in '{1}'.".F(tt, filepath));

				terrainIndexByType.Add(tt, i);
			}

			defaultWalkableTerrainIndex = GetTerrainIndex("Clear");

			// Templates
			Templates = yaml["Templates"].ToDictionary().Values
				.Select(y => new TerrainTemplateInfo(this, y)).ToDictionary(t => t.Id).AsReadOnly();
		}

		public TileSet(string name, string id, TerrainTypeInfo[] terrainInfo)
		{
			Name = name;
			Id = id;
			TerrainInfo = terrainInfo;

			if (TerrainInfo.Length >= byte.MaxValue)
				throw new InvalidDataException("Too many terrain types.");

			for (byte i = 0; i < terrainInfo.Length; i++)
			{
				var tt = terrainInfo[i].Type;
				if (terrainIndexByType.ContainsKey(tt))
					throw new InvalidDataException("Duplicate terrain type '{0}'.".F(tt));

				terrainIndexByType.Add(tt, i);
			}

			defaultWalkableTerrainIndex = GetTerrainIndex("Clear");
		}

		public TerrainTypeInfo this[byte index]
		{
			get { return TerrainInfo[index]; }
		}

		public bool TryGetTerrainIndex(string type, out byte index)
		{
			return terrainIndexByType.TryGetValue(type, out index);
		}

		public byte GetTerrainIndex(string type)
		{
			if (terrainIndexByType.TryGetValue(type, out var index))
				return index;

			throw new InvalidDataException("Tileset '{0}' lacks terrain type '{1}'".F(Id, type));
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

		string ITerrainInfo.Id { get { return Id; } }
		TerrainTypeInfo[] ITerrainInfo.TerrainTypes { get { return TerrainInfo; } }
		TerrainTileInfo ITerrainInfo.GetTerrainInfo(TerrainTile r) { return GetTileInfo(r); }
		bool ITerrainInfo.TryGetTerrainInfo(TerrainTile r, out TerrainTileInfo info) { return TryGetTileInfo(r, out info); }
		Color[] ITerrainInfo.HeightDebugColors { get { return HeightDebugColors; } }
		IEnumerable<Color> ITerrainInfo.RestrictedPlayerColors { get { return TerrainInfo.Where(ti => ti.RestrictPlayerColor).Select(ti => ti.Color); } }
		float ITerrainInfo.MinHeightColorBrightness { get { return MinHeightColorBrightness; } }
		float ITerrainInfo.MaxHeightColorBrightness { get { return MaxHeightColorBrightness; } }
		TerrainTile ITerrainInfo.DefaultTerrainTile { get { return new TerrainTile(Templates.First().Key, 0); } }

		string[] ITemplatedTerrainInfo.EditorTemplateOrder { get { return EditorTemplateOrder; } }
		IReadOnlyDictionary<ushort, TerrainTemplateInfo> ITemplatedTerrainInfo.Templates { get { return Templates; } }

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
