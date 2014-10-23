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
using System.Drawing;
using System.IO;
using System.Linq;

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

		public MiniYaml Save() { return FieldSaver.Save(this); }
	}

	public class TerrainTypeInfo
	{
		public readonly string Type;
		public readonly string[] TargetTypes = { };
		public readonly string[] AcceptsSmudgeType = { };
		public readonly bool IsWater = false; // TODO: Remove this
		public readonly Color Color;
		public readonly string CustomCursor;

		public TerrainTypeInfo() { }
		public TerrainTypeInfo(MiniYaml my) { FieldLoader.Load(this, my); }

		public MiniYaml Save() { return FieldSaver.Save(this); }
	}

	public class TileTemplate
	{
		public readonly ushort Id;
		public readonly string Image;
		public readonly int[] Frames;
		public readonly int2 Size;
		public readonly bool PickAny;
		public readonly string Category;

		TerrainTileInfo[] tileInfo;

		public TileTemplate(ushort id, string image, int2 size, byte[] tiles)
		{
			this.Id = id;
			this.Image = image;
			this.Size = size;
		}

		public TileTemplate(TileSet tileSet, MiniYaml my)
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

		static readonly string[] Fields = { "Id", "Image", "Frames", "Size", "PickAny" };

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
			var root = new List<MiniYamlNode>();
			foreach (var field in Fields)
			{
				var f = this.GetType().GetField(field);
				if (f.GetValue(this) == null)
					continue;

				root.Add(new MiniYamlNode(field, FieldSaver.FormatValue(this, f)));
			}

			root.Add(new MiniYamlNode("Tiles", null,
				tileInfo.Select((terrainTypeIndex, templateIndex) => new MiniYamlNode(templateIndex.ToString(), terrainTypeIndex.Save())).ToList()));

			return new MiniYaml(null, root);
		}
	}

	public class TileSet
	{
		public readonly string Name;
		public readonly string Id;
		public readonly int SheetSize = 512;
		public readonly string Palette;
		public readonly string PlayerPalette;
		public readonly string[] Extensions;
		public readonly int WaterPaletteRotationBase = 0x60; 
		public readonly Dictionary<ushort, TileTemplate> Templates = new Dictionary<ushort, TileTemplate>();
		public readonly string[] EditorTemplateOrder;

		public readonly TerrainTypeInfo[] TerrainInfo;
		readonly Dictionary<string, byte> terrainIndexByType = new Dictionary<string, byte>();
		readonly byte defaultWalkableTerrainIndex;

		static readonly string[] Fields = { "Name", "Id", "SheetSize", "Palette", "Extensions" };

		public TileSet(ModData modData, string filepath)
		{
			var yaml = MiniYaml.DictFromFile(filepath);

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
				.Select(y => new TileTemplate(this, y)).ToDictionary(t => t.Id);
		}

		public TileSet(string name, string id, string palette, string[] extensions, TerrainTypeInfo[] terrainInfo)
		{
			this.Name = name;
			this.Id = id;
			this.Palette = palette;
			this.Extensions = extensions;
			this.TerrainInfo = terrainInfo;
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

		public int TerrainsCount
		{
			get { return TerrainInfo.Length; }
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
			var tpl = Templates[r.Type];

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
			var tpl = Templates[r.Type];
			return tpl.Contains(r.Index) ? tpl[r.Index] : null;
		}

		public void Save(string filepath)
		{
			var root = new List<MiniYamlNode>();
			var gen = new List<MiniYamlNode>();

			foreach (var field in Fields)
			{
				var f = this.GetType().GetField(field);
				if (f.GetValue(this) == null)
					continue;

				gen.Add(new MiniYamlNode(field, FieldSaver.FormatValue(this, f)));
			}

			root.Add(new MiniYamlNode("General", null, gen));

			root.Add(new MiniYamlNode("Terrain", null,
				TerrainInfo.Select(t => new MiniYamlNode("TerrainType@{0}".F(t.Type), t.Save())).ToList()));

			root.Add(new MiniYamlNode("Templates", null,
				Templates.Select(t => new MiniYamlNode("Template@{0}".F(t.Value.Id), t.Value.Save(this))).ToList()));
			root.WriteToFile(filepath);
		}

		public TerrainTypeInfo GetTerrainInfo(TerrainTile r)
		{
			return this[GetTerrainIndex(r)];
		}
	}
}
