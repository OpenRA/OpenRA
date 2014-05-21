#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using OpenRA.Graphics;

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

		[FieldLoader.LoadUsing("LoadTiles")]
		public readonly Dictionary<byte, string> Tiles = new Dictionary<byte, string>();

		public TileTemplate() { }
		public TileTemplate(MiniYaml my) { FieldLoader.Load(this, my); }

		public TileTemplate(ushort id, string image, int2 size)
		{
			this.Id = id;
			this.Image = image;
			this.Size = size;
		}

		static object LoadTiles(MiniYaml y)
		{
			return y.GetNodesDict()["Tiles"].GetNodesDict().ToDictionary(
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
		public readonly string Name;
		public readonly string Id;
		public readonly int SheetSize = 512;
		public readonly string Palette;
		public readonly string PlayerPalette;
		public readonly string[] Extensions;
		public readonly int WaterPaletteRotationBase = 0x60; 
		public readonly Dictionary<string, TerrainTypeInfo> Terrain = new Dictionary<string, TerrainTypeInfo>();
		public readonly Dictionary<ushort, TileTemplate> Templates = new Dictionary<ushort, TileTemplate>();
		public readonly string[] EditorTemplateOrder;

		static readonly string[] Fields = { "Name", "Id", "SheetSize", "Palette", "Extensions" };

		public TileSet(ModData modData, string filepath)
		{
			var yaml = MiniYaml.DictFromFile(filepath);

			// General info
			FieldLoader.Load(this, yaml["General"]);

			// TerrainTypes
			Terrain = yaml["Terrain"].GetNodesDict().Values
				.Select(y => new TerrainTypeInfo(y)).ToDictionary(t => t.Type);

			// Templates
			Templates = yaml["Templates"].GetNodesDict().Values
				.Select(y => new TileTemplate(y)).ToDictionary(t => t.Id);
		}

		public TileSet(string name, string id, string palette, string[] extensions)
		{
			this.Name = name;
			this.Id = id;
			this.Palette = palette;
			this.Extensions = extensions;
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
