#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.MapFormats
{
	public class DefaultMapLoader : IMapLoader
	{
		public readonly string Type;
		public readonly IReadOnlyDictionary<string, MiniYaml> Metadata;

		public Map Load(ModData modData, IReadOnlyPackage package)
		{
			return new DefaultMap(modData, package);
		}

		public Map Create(ModData modData, ITerrainInfo terrainInfo, int width, int height)
		{
			return new DefaultMap(modData, terrainInfo, width, height);
		}

		public string ComputeUID(ModData modData, IReadOnlyPackage package)
		{
			return DefaultMap.ComputeUID(package);
		}

		public void UpdatePreview(ModData modData, MapPreview mp, IReadOnlyPackage p, IReadOnlyPackage parent, MapClassification classification, string[] mapCompatibility, MapGridType gridType)
		{
			DefaultMap.UpdatePreview(modData, mp, p, parent, classification, mapCompatibility, gridType);
		}
	}

	readonly struct BinaryDataHeader
	{
		public readonly byte Format;
		public readonly uint TilesOffset;
		public readonly uint HeightsOffset;
		public readonly uint ResourcesOffset;

		public BinaryDataHeader(Stream s, int2 expectedSize)
		{
			Format = s.ReadUInt8();
			var width = s.ReadUInt16();
			var height = s.ReadUInt16();
			if (width != expectedSize.X || height != expectedSize.Y)
				throw new InvalidDataException("Invalid tile data");

			if (Format == 1)
			{
				TilesOffset = 5;
				HeightsOffset = 0;
				ResourcesOffset = (uint)(3 * width * height + 5);
			}
			else if (Format == 2)
			{
				TilesOffset = s.ReadUInt32();
				HeightsOffset = s.ReadUInt32();
				ResourcesOffset = s.ReadUInt32();
			}
			else
				throw new InvalidDataException($"Unknown binary map format '{Format}'");
		}
	}

	class MapField
	{
		enum Type { Normal, NodeList, MiniYaml }
		readonly FieldInfo field;
		readonly PropertyInfo property;
		readonly Type type;

		readonly string key;
		readonly string fieldName;
		readonly bool required;
		readonly string ignoreIfValue;

		public MapField(string key, string fieldName = null, bool required = true, string ignoreIfValue = null)
		{
			this.key = key;
			this.fieldName = fieldName ?? key;
			this.required = required;
			this.ignoreIfValue = ignoreIfValue;

			field = typeof(DefaultMap).GetField(this.fieldName);
			property = typeof(DefaultMap).GetProperty(this.fieldName);
			if (field == null && property == null)
				throw new InvalidOperationException("Map does not have a field/property " + fieldName);

			var t = field != null ? field.FieldType : property.PropertyType;
			type = t == typeof(List<MiniYamlNode>) ? Type.NodeList :
				t == typeof(MiniYaml) ? Type.MiniYaml : Type.Normal;
		}

		public void Deserialize(Map map, List<MiniYamlNode> nodes)
		{
			var node = nodes.FirstOrDefault(n => n.Key == key);
			if (node == null)
			{
				if (required)
					throw new YamlException($"Required field `{key}` not found in map.yaml");
				return;
			}

			if (field != null)
			{
				if (type == Type.NodeList)
					field.SetValue(map, node.Value.Nodes);
				else if (type == Type.MiniYaml)
					field.SetValue(map, node.Value);
				else
					FieldLoader.LoadField(map, fieldName, node.Value.Value);
			}

			if (property != null)
			{
				if (type == Type.NodeList)
					property.SetValue(map, node.Value.Nodes, null);
				else if (type == Type.MiniYaml)
					property.SetValue(map, node.Value, null);
				else
					FieldLoader.LoadField(map, fieldName, node.Value.Value);
			}
		}

		public void Serialize(Map map, List<MiniYamlNode> nodes)
		{
			var value = field != null ? field.GetValue(map) : property.GetValue(map, null);
			if (type == Type.NodeList)
			{
				var listValue = (List<MiniYamlNode>)value;
				if (required || listValue.Count > 0)
					nodes.Add(new MiniYamlNode(key, null, listValue));
			}
			else if (type == Type.MiniYaml)
			{
				var yamlValue = (MiniYaml)value;
				if (required || (yamlValue != null && (yamlValue.Value != null || yamlValue.Nodes.Count > 0)))
					nodes.Add(new MiniYamlNode(key, yamlValue));
			}
			else
			{
				var formattedValue = FieldSaver.FormatValue(value);
				if (required || formattedValue != ignoreIfValue)
					nodes.Add(new MiniYamlNode(key, formattedValue));
			}
		}
	}

	public class DefaultMap : Map
	{
		public const int SupportedMapFormat = 12;

		/// <summary>Defines the order of the fields in map.yaml</summary>
		static readonly MapField[] YamlFields =
		{
			new MapField("MapFormat"),
			new MapField("RequiresMod"),
			new MapField("Title"),
			new MapField("Author"),
			new MapField("Tileset"),
			new MapField("MapSize"),
			new MapField("Bounds"),
			new MapField("Visibility"),
			new MapField("Categories"),
			new MapField("Translations", required: false, ignoreIfValue: ""),
			new MapField("LockPreview", required: false, ignoreIfValue: "False"),
			new MapField("Players", "PlayerDefinitions"),
			new MapField("Actors", "ActorDefinitions"),
			new MapField("Rules", "RuleDefinitions", required: false),
			new MapField("Sequences", "SequenceDefinitions", required: false),
			new MapField("ModelSequences", "ModelSequenceDefinitions", required: false),
			new MapField("Weapons", "WeaponDefinitions", required: false),
			new MapField("Voices", "VoiceDefinitions", required: false),
			new MapField("Music", "MusicDefinitions", required: false),
			new MapField("Notifications", "NotificationDefinitions", required: false),
			//new MapField("Translations", "TranslationDefinitions", required: false)
		};

		// Format versions
		public int MapFormat { get; private set; }
		public readonly byte TileFormat = 2;

		public static string ComputeUID(IReadOnlyPackage package)
		{
			return ComputeUID(package, GetMapFormat(package));
		}

		public static string ComputeUID(IReadOnlyPackage package, int format)
		{
			// UID is calculated by taking an SHA1 of the yaml and binary data
			var requiredFiles = new[] { "map.yaml", "map.bin" };
			var contents = package.Contents.ToList();
			foreach (var required in requiredFiles)
				if (!contents.Contains(required))
					throw new FileNotFoundException($"Required file {required} not present in this map");

			var streams = new List<Stream>();
			try
			{
				foreach (var filename in contents)
					if (filename.EndsWith(".yaml") || filename.EndsWith(".bin") || filename.EndsWith(".lua") || (format >= 12 && filename == "map.png"))
						streams.Add(package.GetStream(filename));

				// Take the SHA1
				if (streams.Count == 0)
					return CryptoUtil.SHA1Hash(Array.Empty<byte>());

				var merged = streams[0];
				for (var i = 1; i < streams.Count; i++)
					merged = new MergedStream(merged, streams[i]);

				return CryptoUtil.SHA1Hash(merged);
			}
			finally
			{
				foreach (var stream in streams)
					stream.Dispose();
			}
		}

		/// <summary>
		/// Initializes a new map created by the editor or importer.
		/// The map will not receive a valid UID until after it has been saved and reloaded.
		/// </summary>
		public DefaultMap(ModData modData, ITerrainInfo terrainInfo, int width, int height)
			: base(modData)
		{
			var tileRef = terrainInfo.DefaultTerrainTile;
			var size = new Size(width, height);

			Title = "Name your map here";
			Author = "Your name here";

			MapSize = new int2(size);
			Tileset = terrainInfo.Id;

			NewSize(new Size(width, height), terrainInfo);

			Tiles.Clear(tileRef);

			PostInit();
		}

		public DefaultMap(ModData modData, IReadOnlyPackage package)
			: base(modData)
		{
			Package = package;

			if (!Package.Contains("map.yaml") || !Package.Contains("map.bin"))
				throw new InvalidDataException($"Not a valid map\n File: {package.Name}");

			var yaml = new MiniYaml(null, MiniYaml.FromStream(Package.GetStream("map.yaml"), package.Name));
			foreach (var field in YamlFields)
				field.Deserialize(this, yaml.Nodes);

			if (MapFormat < SupportedMapFormat)
				throw new InvalidDataException($"Map format {MapFormat} is not supported.\n File: {package.Name}");

			PlayerDefinitions = MiniYaml.NodesOrEmpty(yaml, "Players");
			ActorDefinitions = MiniYaml.NodesOrEmpty(yaml, "Actors");

			Ruleset rules;
			try
			{
				rules = Ruleset.Load(modData, this, Tileset, RuleDefinitions, WeaponDefinitions,
					VoiceDefinitions, NotificationDefinitions, MusicDefinitions, ModelSequenceDefinitions);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to load rules for {0} with error {1}", Title, e);
				rules = Ruleset.LoadDefaultsForTileSet(modData, Tileset);
			}

			var size = new Size(MapSize.X, MapSize.Y);
			Tiles = new CellLayer<TerrainTile>(Grid.Type, size);
			Resources = new CellLayer<ResourceTile>(Grid.Type, size);
			Height = new CellLayer<byte>(Grid.Type, size);
			Ramp = new CellLayer<byte>(Grid.Type, size);

			using (var s = Package.GetStream("map.bin"))
			{
				var header = new BinaryDataHeader(s, MapSize);
				if (header.TilesOffset > 0)
				{
					s.Position = header.TilesOffset;
					for (var i = 0; i < MapSize.X; i++)
					{
						for (var j = 0; j < MapSize.Y; j++)
						{
							var tile = s.ReadUInt16();
							var index = s.ReadUInt8();

							// TODO: Remember to remove this when rewriting tile variants / PickAny
							if (index == byte.MaxValue)
								index = (byte)(i % 4 + j % 4 * 4);

							Tiles[new MPos(i, j)] = new TerrainTile(tile, index);
						}
					}
				}

				if (header.ResourcesOffset > 0)
				{
					s.Position = header.ResourcesOffset;
					for (var i = 0; i < MapSize.X; i++)
					{
						for (var j = 0; j < MapSize.Y; j++)
						{
							var type = s.ReadUInt8();
							var density = s.ReadUInt8();
							Resources[new MPos(i, j)] = new ResourceTile(type, density);
						}
					}
				}

				if (header.HeightsOffset > 0)
				{
					s.Position = header.HeightsOffset;
					for (var i = 0; i < MapSize.X; i++)
						for (var j = 0; j < MapSize.Y; j++)
							Height[new MPos(i, j)] = s.ReadUInt8().Clamp((byte)0, Grid.MaximumTerrainHeight);
				}
			}

			PostInit();

			Uid = ComputeUID(Package);
		}

		public override void Save(IReadWritePackage toPackage)
		{
			MapFormat = SupportedMapFormat;

			var root = new List<MiniYamlNode>();
			foreach (var field in YamlFields)
				field.Serialize(this, root);

			// HACK: map.yaml is expected to have empty lines between top-level blocks
			for (var i = root.Count - 1; i > 0; i--)
				root.Insert(i, new MiniYamlNode("", ""));

			// Saving to a new package: copy over all the content from the map
			if (Package != null && toPackage != Package)
				foreach (var file in Package.Contents)
					toPackage.Update(file, Package.GetStream(file).ReadAllBytes());

			if (!LockPreview)
			{
				var previewData = SavePreview();
				if (Package != toPackage || !Enumerable.SequenceEqual(previewData, Package.GetStream("map.png").ReadAllBytes()))
					toPackage.Update("map.png", previewData);
			}

			// Update the package with the new map data
			var textData = Encoding.UTF8.GetBytes(root.WriteToString());
			if (Package != toPackage || !Enumerable.SequenceEqual(textData, Package.GetStream("map.yaml").ReadAllBytes()))
				toPackage.Update("map.yaml", textData);

			var binaryData = SaveBinaryData();
			if (Package != toPackage || !Enumerable.SequenceEqual(binaryData, Package.GetStream("map.bin").ReadAllBytes()))
				toPackage.Update("map.bin", binaryData);

			// Update UID to match the newly saved data
			Uid = ComputeUID(toPackage);
		}

		public static void UpdatePreview(ModData modData, MapPreview mp, IReadOnlyPackage p, IReadOnlyPackage parent, MapClassification classification, string[] mapCompatibility, MapGridType gridType)
		{
			Dictionary<string, MiniYaml> yaml;
			using (var yamlStream = p.GetStream("map.yaml"))
			{
				if (yamlStream == null)
					throw new FileNotFoundException("Required file map.yaml not present in this map");

				yaml = new MiniYaml(null, MiniYaml.FromStream(yamlStream, "map.yaml", stringPool: modData.MapCache.StringPool)).ToDictionary();
			}

			var newData = mp.Init(p, parent);
			newData.GridType = gridType;
			newData.Class = classification;

			if (yaml.TryGetValue("MapFormat", out var temp))
			{
				var format = FieldLoader.GetValue<int>("MapFormat", temp.Value);
				if (format != SupportedMapFormat)
					throw new InvalidDataException("Map format {0} is not supported.".F(format));
			}

			if (yaml.TryGetValue("Title", out temp))
				newData.Title = temp.Value;

			if (yaml.TryGetValue("Categories", out temp))
				newData.Categories = FieldLoader.GetValue<string[]>("Categories", temp.Value);

			if (yaml.TryGetValue("Tileset", out temp))
				newData.TileSet = temp.Value;

			if (yaml.TryGetValue("Author", out temp))
				newData.Author = temp.Value;

			if (yaml.TryGetValue("Bounds", out temp))
				newData.Bounds = FieldLoader.GetValue<Rectangle>("Bounds", temp.Value);

			if (yaml.TryGetValue("Visibility", out temp))
				newData.Visibility = FieldLoader.GetValue<MapVisibility>("Visibility", temp.Value);

			var requiresMod = string.Empty;
			if (yaml.TryGetValue("RequiresMod", out temp))
				requiresMod = temp.Value;

			newData.Status = mapCompatibility == null || mapCompatibility.Contains(requiresMod) ?
				MapStatus.Available : MapStatus.Unavailable;

			try
			{
				// Actor definitions may change if the map format changes
				if (yaml.TryGetValue("Actors", out var actorDefinitions))
				{
					var spawns = new List<CPos>();
					foreach (var kv in actorDefinitions.Nodes.Where(d => d.Value.Value == "mpspawn"))
					{
						var s = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						spawns.Add(s.Get<LocationInit>().Value);
					}

					newData.SpawnPoints = spawns.ToArray();
				}
				else
					newData.SpawnPoints = Array.Empty<CPos>();
			}
			catch (Exception)
			{
				newData.SpawnPoints = Array.Empty<CPos>();
				newData.Status = MapStatus.Unavailable;
			}

			try
			{
				// Player definitions may change if the map format changes
				if (yaml.TryGetValue("Players", out var playerDefinitions))
				{
					newData.Players = new MapPlayers(playerDefinitions.Nodes);
					newData.PlayerCount = newData.Players.Players.Count(x => x.Value.Playable);
				}
			}
			catch (Exception)
			{
				newData.Status = MapStatus.Unavailable;
			}

			newData.SetCustomRules(modData, mp, yaml);

			if (mp.Cache.LoadPreviewImages && p.Contains("map.png"))
				using (var dataStream = p.GetStream("map.png"))
					newData.Preview = new Png(dataStream);

			newData.ModifiedDate = File.GetLastWriteTime(p.Name);
		}

		public byte[] SaveBinaryData()
		{
			var dataStream = new MemoryStream();
			using (var writer = new BinaryWriter(dataStream))
			{
				// Binary data version
				writer.Write(TileFormat);

				// Size
				writer.Write((ushort)MapSize.X);
				writer.Write((ushort)MapSize.Y);

				// Data offsets
				var tilesOffset = 17;
				var heightsOffset = Grid.MaximumTerrainHeight > 0 ? 3 * MapSize.X * MapSize.Y + 17 : 0;
				var resourcesOffset = (Grid.MaximumTerrainHeight > 0 ? 4 : 3) * MapSize.X * MapSize.Y + 17;

				writer.Write((uint)tilesOffset);
				writer.Write((uint)heightsOffset);
				writer.Write((uint)resourcesOffset);

				// Tile data
				if (tilesOffset != 0)
				{
					for (var i = 0; i < MapSize.X; i++)
					{
						for (var j = 0; j < MapSize.Y; j++)
						{
							var tile = Tiles[new MPos(i, j)];
							writer.Write(tile.Type);
							writer.Write(tile.Index);
						}
					}
				}

				// Height data
				if (heightsOffset != 0)
					for (var i = 0; i < MapSize.X; i++)
						for (var j = 0; j < MapSize.Y; j++)
							writer.Write(Height[new MPos(i, j)]);

				// Resource data
				if (resourcesOffset != 0)
				{
					for (var i = 0; i < MapSize.X; i++)
					{
						for (var j = 0; j < MapSize.Y; j++)
						{
							var tile = Resources[new MPos(i, j)];
							writer.Write(tile.Type);
							writer.Write(tile.Index);
						}
					}
				}
			}

			return dataStream.ToArray();
		}
	}
}
