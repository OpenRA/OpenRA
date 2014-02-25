#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace OpenRA.FileFormats
{
	public class TerrainTypeInfo
	{
		public string Type;
		public string[] TargetTypes = { };
		public string[] AcceptsSmudgeType = { };
		public bool IsWater = false; // TODO: Remove this
		public Color Color;
		public string CustomCursor;

		public TerrainTypeInfo() { }
		public TerrainTypeInfo(MiniYaml my) { FieldLoader.Load(this, my); }

		public MiniYaml Save() { return FieldSaver.Save(this); }
	}

	public class TileTemplate
	{
		public ushort Id;
		public string Image;
		public int[] Frames;
		public int2 Size;
		public bool PickAny;
		public string Category;

		[FieldLoader.LoadUsing("LoadTiles")]
		public Dictionary<byte, string> Tiles = new Dictionary<byte, string>();

		public TileTemplate() { }
		public TileTemplate(MiniYaml my) { FieldLoader.Load(this, my); }

		static object LoadTiles(MiniYaml y)
		{
			return y.NodesDict["Tiles"].NodesDict.ToDictionary(
				t => byte.Parse(t.Key),
				t => t.Value.Value);
		}

		static readonly string[] Fields = { "Id", "Image", "Frames", "Size", "PickAny" };

		public MiniYaml Save()
		{
			var root = new List<MiniYamlNode>();
			foreach (var field in Fields)
			{
				FieldInfo f = this.GetType().GetField(field);
				if (f.GetValue(this) == null)
					continue;

				root.Add(new MiniYamlNode(field, FieldSaver.FormatValue(this, f)));
			}

			root.Add(new MiniYamlNode("Tiles", null,
				Tiles.Select(x => new MiniYamlNode(x.Key.ToString(), x.Value)).ToList()));

			return new MiniYaml(null, root);
		}
	}

	public class TileSet
	{
		public string Name;
		public string Id;
		public int SheetSize = 512;
		public string Palette;
		public string PlayerPalette;
		public string[] Extensions;
		public int WaterPaletteRotationBase = 0x60; 
		public Dictionary<string, TerrainTypeInfo> Terrain = new Dictionary<string, TerrainTypeInfo>();
		public Dictionary<ushort, TileTemplate> Templates = new Dictionary<ushort, TileTemplate>();
		public string[] EditorTemplateOrder;

		static readonly string[] Fields = { "Name", "Id", "SheetSize", "Palette", "Extensions" };

		public TileSet() { }

		public TileSet(string filepath)
		{
			var yaml = MiniYaml.DictFromFile(filepath);

			// General info
			FieldLoader.Load(this, yaml["General"]);

			// TerrainTypes
			Terrain = yaml["Terrain"].NodesDict.Values
				.Select(y => new TerrainTypeInfo(y)).ToDictionary(t => t.Type);

			// Templates
			Templates = yaml["Templates"].NodesDict.Values
				.Select(y => new TileTemplate(y)).ToDictionary(t => t.Id);
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
				Terrain.Select(t => new MiniYamlNode("TerrainType@{0}".F(t.Value.Type), t.Value.Save())).ToList()));

			root.Add(new MiniYamlNode("Templates", null,
				Templates.Select(t => new MiniYamlNode("Template@{0}".F(t.Value.Id), t.Value.Save())).ToList()));
			root.WriteToFile(filepath);
		}

		public string GetTerrainType(TileReference<ushort, byte> r)
		{
			var tt = Templates[r.Type].Tiles;
			string ret;
			if (!tt.TryGetValue(r.Index, out ret))
				return "Clear"; // Default walkable

			return ret;
		}
	}
}
