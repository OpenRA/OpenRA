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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA
{
	public class TerrainTileInfo
	{
		[FieldLoader.Ignore]
		public readonly byte TerrainType = byte.MaxValue;
		public readonly byte Height;
		public readonly byte RampType;
		public readonly Color LeftColor;
		public readonly Color RightColor;

		public readonly float ZOffset = 0.0f;
		public readonly float ZRamp = 1.0f;

		public MiniYaml Save(TileSet tileSet)
		{
			var root = new List<MiniYamlNode>();
			if (Height != 0)
				root.Add(FieldSaver.SaveField(this, "Height"));

			if (RampType != 0)
				root.Add(FieldSaver.SaveField(this, "RampType"));

			if (LeftColor != tileSet.TerrainInfo[TerrainType].Color)
				root.Add(FieldSaver.SaveField(this, "LeftColor"));

			if (RightColor != tileSet.TerrainInfo[TerrainType].Color)
				root.Add(FieldSaver.SaveField(this, "RightColor"));

			if (ZOffset != 0.0f)
				root.Add(FieldSaver.SaveField(this, "ZOffset"));

			if (ZRamp != 1.0f)
				root.Add(FieldSaver.SaveField(this, "ZRamp"));

			return new MiniYaml(tileSet.TerrainInfo[TerrainType].Type, root);
		}
	}

	public class TerrainTypeInfo
	{
		static readonly TerrainTypeInfo Default = new TerrainTypeInfo();

		public readonly string Type;
		public readonly HashSet<string> TargetTypes = new HashSet<string>();
		public readonly HashSet<string> AcceptsSmudgeType = new HashSet<string>();
		public readonly bool IsWater = false; // TODO: Remove this
		public readonly Color Color;
		public readonly bool RestrictPlayerColor = false;
		public readonly string CustomCursor;

		// Private default ctor for serialization comparison
		TerrainTypeInfo() { }

		public TerrainTypeInfo(MiniYaml my) { FieldLoader.Load(this, my); }

		public MiniYaml Save() { return FieldSaver.SaveDifferences(this, Default); }
	}

	public class TerrainTemplateInfo
	{
		static readonly TerrainTemplateInfo Default = new TerrainTemplateInfo(0, new string[] { null }, int2.Zero, null);

		public readonly ushort Id;
		public readonly string[] Images;
		public readonly int[] Frames;
		public readonly int2 Size;
		public readonly bool PickAny;
		public readonly string Category;
		public readonly string Palette;

		readonly TerrainTileInfo[] tileInfo;

		public TerrainTemplateInfo(ushort id, string[] images, int2 size, byte[] tiles)
		{
			Id = id;
			Images = images;
			Size = size;
		}

		public TerrainTemplateInfo(TileSet tileSet, MiniYaml my)
		{
			FieldLoader.Load(this, my);

			var nodes = my.ToDictionary()["Tiles"].Nodes;

			if (!PickAny)
			{
				tileInfo = new TerrainTileInfo[Size.X * Size.Y];
				foreach (var node in nodes)
				{
					int key;
					if (!int.TryParse(node.Key, out key) || key < 0 || key >= tileInfo.Length)
						throw new InvalidDataException("Invalid tile key '{0}' on template '{1}' of tileset '{2}'.".F(node.Key, Id, tileSet.Id));

					tileInfo[key] = LoadTileInfo(tileSet, node.Value);
				}
			}
			else
			{
				tileInfo = new TerrainTileInfo[nodes.Count];

				var i = 0;
				foreach (var node in nodes)
				{
					int key;
					if (!int.TryParse(node.Key, out key) || key != i++)
						throw new InvalidDataException("Invalid tile key '{0}' on template '{1}' of tileset '{2}'.".F(node.Key, Id, tileSet.Id));

					tileInfo[key] = LoadTileInfo(tileSet, node.Value);
				}
			}
		}

		static TerrainTileInfo LoadTileInfo(TileSet tileSet, MiniYaml my)
		{
			var tile = new TerrainTileInfo();
			FieldLoader.Load(tile, my);

			// Terrain type must be converted from a string to an index
			tile.GetType().GetField("TerrainType").SetValue(tile, tileSet.GetTerrainIndex(my.Value));

			// Fall back to the terrain-type color if necessary
			var overrideColor = tileSet.TerrainInfo[tile.TerrainType].Color;
			if (tile.LeftColor == default(Color))
				tile.GetType().GetField("LeftColor").SetValue(tile, overrideColor);

			if (tile.RightColor == default(Color))
				tile.GetType().GetField("RightColor").SetValue(tile, overrideColor);

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

		public MiniYaml Save(TileSet tileSet)
		{
			var root = FieldSaver.SaveDifferences(this, Default);

			var tileYaml = tileInfo
				.Select((ti, i) => Pair.New(i.ToString(), ti))
				.Where(t => t.Second != null)
				.Select(t => new MiniYamlNode(t.First, t.Second.Save(tileSet)))
				.ToList();

			root.Nodes.Add(new MiniYamlNode("Tiles", null, tileYaml));

			return root;
		}
	}

	public class TileSet
	{
		public const string TerrainPaletteInternalName = "terrain";

		public readonly string Name;
		public readonly string Id;
		public readonly int SheetSize = 512;
		public readonly string Palette;
		public readonly string PlayerPalette;
		public readonly Color[] HeightDebugColors = new[] { Color.Red };
		public readonly string[] EditorTemplateOrder;
		public readonly bool IgnoreTileSpriteOffsets;
		public readonly bool EnableDepth = false;

		[FieldLoader.Ignore]
		public readonly IReadOnlyDictionary<ushort, TerrainTemplateInfo> Templates;

		[FieldLoader.Ignore]
		public readonly TerrainTypeInfo[] TerrainInfo;
		readonly Dictionary<string, byte> terrainIndexByType = new Dictionary<string, byte>();
		readonly byte defaultWalkableTerrainIndex;

		// Private default ctor for serialization comparison
		TileSet() { }

		public TileSet(IReadOnlyFileSystem fileSystem, string filepath)
		{
			var yaml = MiniYaml.DictFromStream(fileSystem.Open(filepath), filepath);

			// General info
			FieldLoader.Load(this, yaml["General"]);

			// TerrainTypes
			TerrainInfo = yaml["Terrain"].ToDictionary().Values
				.Select(y => new TerrainTypeInfo(y))
				.OrderBy(tt => tt.Type)
				.ToArray();

			if (TerrainInfo.Length >= byte.MaxValue)
				throw new InvalidDataException("Too many terrain types.");

			for (byte i = 0; i < TerrainInfo.Length; i++)
			{
				var tt = TerrainInfo[i].Type;

				if (terrainIndexByType.ContainsKey(tt))
					throw new InvalidDataException("Duplicate terrain type '{0}' in '{1}'.".F(tt, filepath));

				terrainIndexByType.Add(tt, i);
			}

			defaultWalkableTerrainIndex = GetTerrainIndex("Clear");

			// Templates
			Templates = yaml["Templates"].ToDictionary().Values
				.Select(y => new TerrainTemplateInfo(this, y)).ToDictionary(t => t.Id).AsReadOnly();
		}

		public TileSet(string name, string id, string palette, TerrainTypeInfo[] terrainInfo)
		{
			Name = name;
			Id = id;
			Palette = palette;
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
			byte index;
			if (terrainIndexByType.TryGetValue(type, out index))
				return index;

			throw new InvalidDataException("Tileset '{0}' lacks terrain type '{1}'".F(Id, type));
		}

		public byte GetTerrainIndex(TerrainTile r)
		{
			TerrainTemplateInfo tpl;
			if (!Templates.TryGetValue(r.Type, out tpl))
				return defaultWalkableTerrainIndex;

			if (tpl.Contains(r.Index))
			{
				var tile = tpl[r.Index];
				if (tile != null && tile.TerrainType != byte.MaxValue)
					return tile.TerrainType;
			}

			return defaultWalkableTerrainIndex;
		}

		public TerrainTileInfo GetTileInfo(TerrainTile r)
		{
			TerrainTemplateInfo tpl;
			if (!Templates.TryGetValue(r.Type, out tpl))
				return null;

			return tpl.Contains(r.Index) ? tpl[r.Index] : null;
		}

		public void Save(string filepath)
		{
			var root = new List<MiniYamlNode>();
			root.Add(new MiniYamlNode("General", FieldSaver.SaveDifferences(this, new TileSet())));

			root.Add(new MiniYamlNode("Terrain", null,
				TerrainInfo.Select(t => new MiniYamlNode("TerrainType@{0}".F(t.Type), t.Save())).ToList()));

			root.Add(new MiniYamlNode("Templates", null,
				Templates.Select(t => new MiniYamlNode("Template@{0}".F(t.Value.Id), t.Value.Save(this))).ToList()));
			root.WriteToFile(filepath);
		}
	}
}
