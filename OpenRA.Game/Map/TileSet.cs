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

		int[] tiles;

		public TileTemplate(ushort id, string image, int2 size, int[] tiles)
		{
			this.Id = id;
			this.Image = image;
			this.Size = size;
			this.tiles = tiles;
		}

		public TileTemplate(TileSet tileSet, MiniYaml my)
		{
			FieldLoader.Load(this, my);

			tiles = LoadTiles(tileSet, my);
		}

		int[] LoadTiles(TileSet tileSet, MiniYaml y)
		{
			var nodes = y.ToDictionary()["Tiles"].Nodes;

			if (!PickAny)
			{
				var tiles = new int[Size.X * Size.Y];

				for (var i = 0; i < tiles.Length; i++)
					tiles[i] = -1;

				foreach (var node in nodes)
				{
					int key;
					if (!int.TryParse(node.Key, out key) || key < 0 || key >= tiles.Length)
						throw new InvalidDataException("Invalid tile key '{0}' on template '{1}' of tileset '{2}'.".F(node.Key, Id, tileSet.Id));

					tiles[key] = tileSet.GetTerrainIndex(node.Value.Value);
				}

				return tiles;
			}
			else
			{
				var tiles = new int[nodes.Count];
				var i = 0;

				foreach (var node in nodes)
				{
					int key;
					if (!int.TryParse(node.Key, out key) || key != i++)
						throw new InvalidDataException("Invalid tile key '{0}' on template '{1}' of tileset '{2}'.".F(node.Key, Id, tileSet.Id));

					tiles[key] = tileSet.GetTerrainIndex(node.Value.Value);
				}

				return tiles;
			}
		}

		static readonly string[] Fields = { "Id", "Image", "Frames", "Size", "PickAny" };

		public int this[int index]
		{
			get { return tiles[index]; }
		}

		public bool Contains(int index)
		{
			return index >= 0 && index < tiles.Length;
		}

		public int TilesCount
		{
			get { return tiles.Length; }
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
				tiles.Select((terrainTypeIndex, templateIndex) => new MiniYamlNode(templateIndex.ToString(), tileSet[terrainTypeIndex].Type)).ToList()));

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

		readonly TerrainTypeInfo[] terrainInfo;
		readonly Dictionary<string, int> terrainIndexByType = new Dictionary<string, int>();
		readonly int defaultWalkableTerrainIndex;

		static readonly string[] Fields = { "Name", "Id", "SheetSize", "Palette", "Extensions" };

		public TileSet(ModData modData, string filepath)
		{
			var yaml = MiniYaml.DictFromFile(filepath);

			// General info
			FieldLoader.Load(this, yaml["General"]);

			// TerrainTypes
			terrainInfo = yaml["Terrain"].ToDictionary().Values
				.Select(y => new TerrainTypeInfo(y))
				.OrderBy(tt => tt.Type)
				.ToArray();
			for (var i = 0; i < terrainInfo.Length; i++)
			{
				var tt = terrainInfo[i].Type;

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
			this.terrainInfo = terrainInfo;

			for (var i = 0; i < terrainInfo.Length; i++)
			{
				var tt = terrainInfo[i].Type;

				if (terrainIndexByType.ContainsKey(tt))
					throw new InvalidDataException("Duplicate terrain type '{0}'.".F(tt));

				terrainIndexByType.Add(tt, i);
			}
			defaultWalkableTerrainIndex = GetTerrainIndex("Clear");
		}

		public TerrainTypeInfo this[int index]
		{
			get { return terrainInfo[index]; }
		}

		public int TerrainsCount
		{
			get { return terrainInfo.Length; }
		}

		public bool TryGetTerrainIndex(string type, out int index)
		{
			return terrainIndexByType.TryGetValue(type, out index);
		}

		public int GetTerrainIndex(string type)
		{
			int index;
			if (terrainIndexByType.TryGetValue(type, out index))
				return index;

			throw new InvalidDataException("Tileset '{0}' lacks terrain type '{1}'".F(Id, type));
		}

		public int GetTerrainIndex(TerrainTile r)
		{
			var tpl = Templates[r.Type];

			if (tpl.Contains(r.Index))
			{
				var ti = tpl[r.Index];
				if (ti != -1)
					return ti;
			}

			return defaultWalkableTerrainIndex;
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
				terrainInfo.Select(t => new MiniYamlNode("TerrainType@{0}".F(t.Type), t.Save())).ToList()));

			root.Add(new MiniYamlNode("Templates", null,
				Templates.Select(t => new MiniYamlNode("Template@{0}".F(t.Value.Id), t.Value.Save(this))).ToList()));
			root.WriteToFile(filepath);
		}

		public TerrainTypeInfo GetTerrainInfo(TerrainTile r)
		{
			return terrainInfo[GetTerrainIndex(r)];
		}
	}
}
