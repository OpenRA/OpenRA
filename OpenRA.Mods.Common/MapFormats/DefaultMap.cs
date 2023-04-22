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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

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

		public void Deserialize(IMap map, List<MiniYamlNode> nodes)
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

		public void Serialize(IMap map, List<MiniYamlNode> nodes)
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

	public class DefaultMap : Map, IMap, IMapResource, IMapElevation, IMapTiles, IDisposable
	{
		public const int SupportedMapFormat = 11;
		public const int CurrentMapFormat = 12;

		protected CellLayer<short> cachedTerrainIndexes;
		protected bool initializedCellProjection;
		protected CellLayer<PPos[]> cellProjection;
		protected CellLayer<List<MPos>> inverseCellProjection;
		protected CellLayer<byte> projectedHeight;
		protected Rectangle projectionSafeBounds;

		/// <summary>
		/// The top-left of the playable area in projected world coordinates
		/// This is a hacky workaround for legacy functionality.  Do not use for new code.
		/// </summary>
		public WPos ProjectedTopLeft { get; private set; }

		/// <summary>
		/// The bottom-right of the playable area in projected world coordinates
		/// This is a hacky workaround for legacy functionality.  Do not use for new code.
		/// </summary>
		public WPos ProjectedBottomRight { get; private set; }

		public CellLayer<TerrainTile> Tiles { get; protected set; }
		public string Tileset;
		string IMap.Tileset => Tileset;
		public Ruleset Rules { get; protected set; }
		public SequenceSet Sequences { get; private set; }
		Ruleset IMap.Rules => Rules;
		public CellLayer<ResourceTile> Resources { get; protected set; }
		public CellLayer<byte> Height { get; protected set; }
		public CellLayer<byte> Ramp { get; protected set; }
		public CellLayer<byte> CustomTerrain { get; private set; }

		public PPos[] ProjectedCells { get; private set; }
		public CellRegion AllCells { get; private set; }
		public List<CPos> AllEdgeCells { get; private set; }

		public event Action<CPos> CellProjectionChanged;
		Dictionary<CPos, TerrainTile> IMap.ReplacedInvalidTerrainTiles => ReplacedInvalidTerrainTiles;
		public int2 MapSize { get; protected set; }
		MapGrid IMap.Grid => Grid;
		int2 IMap.MapSize => MapSize;
		protected CVec[][] GridTilesByDistance => Grid.TilesByDistance;
		public Rectangle Bounds;
		Rectangle IMap.Bounds => Bounds;

		// Generated data

		/// <summary>Defines the order of the fields in map.yaml.</summary>
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
		};

		// Format versions
		public int MapFormat { get; private set; }
		int IMap.MapFormat => MapFormat;

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
				throw new InvalidDataException("Map format {0} is not supported.\n File: {1}".F(MapFormat, package.Name));

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

			Uid = ComputeUID(Package, MapFormat);
		}

		protected void PostInit()
		{
			try
			{
				Rules = Ruleset.Load(modData, this, Tileset, RuleDefinitions, WeaponDefinitions,
					VoiceDefinitions, NotificationDefinitions, MusicDefinitions, ModelSequenceDefinitions);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to load rules for {0} with error {1}", Title, e);
				InvalidCustomRules = true;
				InvalidCustomRulesException = e;
				Rules = Ruleset.LoadDefaultsForTileSet(modData, Tileset);
			}

			Sequences = new SequenceSet(this, modData, Tileset, SequenceDefinitions);
			Translation = new Translation(Game.Settings.Player.Language, Translations, this);

			var tl = new MPos(0, 0).ToCPos(this);
			var br = new MPos(MapSize.X - 1, MapSize.Y - 1).ToCPos(this);
			AllCells = new CellRegion(Grid.Type, tl, br);

			var btl = new PPos(Bounds.Left, Bounds.Top);
			var bbr = new PPos(Bounds.Right - 1, Bounds.Bottom - 1);
			SetBounds(btl, bbr);

			CustomTerrain = new CellLayer<byte>(this);
			foreach (var uv in AllCells.MapCoords)
				CustomTerrain[uv] = byte.MaxValue;

			// Replace invalid tiles and cache ramp state
			var terrainInfo = Rules.TerrainInfo;
			foreach (var uv in AllCells.MapCoords)
			{
				if (!terrainInfo.TryGetTerrainInfo(Tiles[uv], out var info))
				{
					ReplacedInvalidTerrainTiles[uv.ToCPos(this)] = Tiles[uv];
					Tiles[uv] = terrainInfo.DefaultTerrainTile;
					info = terrainInfo.GetTerrainInfo(terrainInfo.DefaultTerrainTile);
				}

				Ramp[uv] = info.RampType;
			}

			AllEdgeCells = UpdateEdgeCells();

			// Invalidate the entry for a cell if anything could cause the terrain index to change.
			void InvalidateTerrainIndex(CPos c)
			{
				if (cachedTerrainIndexes != null)
					cachedTerrainIndexes[c] = InvalidCachedTerrainIndex;
			}

			// Even though the cache is lazily initialized, we must attach these event handlers on init.
			// This ensures our handler to invalidate the cache runs first,
			// so other listeners to these same events will get correct data when calling GetTerrainIndex.
			CustomTerrain.CellEntryChanged += InvalidateTerrainIndex;
			Tiles.CellEntryChanged += InvalidateTerrainIndex;
		}

		public (Color Left, Color Right) GetTerrainColorPair(MPos uv)
		{
			var terrainInfo = Rules.TerrainInfo;
			var type = terrainInfo.GetTerrainInfo(Tiles[uv]);
			var left = type.GetColor(Game.CosmeticRandom);
			var right = type.GetColor(Game.CosmeticRandom);

			if (terrainInfo.MinHeightColorBrightness != 1.0f || terrainInfo.MaxHeightColorBrightness != 1.0f)
			{
				var scale = float2.Lerp(terrainInfo.MinHeightColorBrightness, terrainInfo.MaxHeightColorBrightness, Height[uv] * 1f / Grid.MaximumTerrainHeight);
				left = Color.FromArgb((int)(scale * left.R).Clamp(0, 255), (int)(scale * left.G).Clamp(0, 255), (int)(scale * left.B).Clamp(0, 255));
				right = Color.FromArgb((int)(scale * right.R).Clamp(0, 255), (int)(scale * right.G).Clamp(0, 255), (int)(scale * right.B).Clamp(0, 255));
			}

			return (left, right);
		}

		public byte[] SavePreview()
		{
			var actorTypes = Rules.Actors.Values.Where(a => a.HasTraitInfo<IMapPreviewSignatureInfo>());
			var actors = ActorDefinitions.Where(a => actorTypes.Any(ai => ai.Name == a.Value.Value));
			var positions = new List<(MPos Position, Color Color)>();
			foreach (var actor in actors)
			{
				var s = new ActorReference(actor.Value.Value, actor.Value.ToDictionary());

				var ai = Rules.Actors[actor.Value.Value];
				var impsis = ai.TraitInfos<IMapPreviewSignatureInfo>();
				foreach (var impsi in impsis)
					impsi.PopulateMapPreviewSignatureCells(this, ai, s, positions);
			}

			// ResourceLayer is on world actor, which isn't caught above, so an extra check for it.
			var worldActorInfo = Rules.Actors[SystemActors.World];
			var worldimpsis = worldActorInfo.TraitInfos<IMapPreviewSignatureInfo>();
			foreach (var worldimpsi in worldimpsis)
				worldimpsi.PopulateMapPreviewSignatureCells(this, worldActorInfo, null, positions);

			var isRectangularIsometric = Grid.Type == MapGridType.RectangularIsometric;

			var top = int.MaxValue;
			var bottom = int.MinValue;

			if (Grid.MaximumTerrainHeight > 0)
			{
				// The minimap is drawn in cell space, so we need to
				// unproject the PPos bounds to find the MPos boundaries.
				// This matches the calculation in RadarWidget that is used ingame
				for (var x = Bounds.Left; x < Bounds.Right; x++)
				{
					var allTop = Unproject(new PPos(x, Bounds.Top));
					var allBottom = Unproject(new PPos(x, Bounds.Bottom));
					if (allTop.Count > 0)
						top = Math.Min(top, allTop.MinBy(uv => uv.V).V);

					if (allBottom.Count > 0)
						bottom = Math.Max(bottom, allBottom.MaxBy(uv => uv.V).V);
				}
			}
			else
			{
				// If the mod uses flat maps, MPos == PPos and we can take the bounds rect directly
				top = Bounds.Top;
				bottom = Bounds.Bottom;
			}

			var width = Bounds.Width;
			var height = bottom - top;

			var bitmapWidth = width;
			if (isRectangularIsometric)
				bitmapWidth = 2 * bitmapWidth - 1;

			var stride = bitmapWidth * 4;
			var pxStride = 4;
			var minimapData = new byte[stride * height];
			(Color Left, Color Right) terrainColor = default;

			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var uv = new MPos(x + Bounds.Left, y + top);

					// FirstOrDefault will return a (MPos.Zero, Color.Transparent) if positions is empty
					var actorColor = positions.FirstOrDefault(ap => ap.Position == uv).Color;
					if (actorColor.A == 0)
						terrainColor = GetTerrainColorPair(uv);

					if (isRectangularIsometric)
					{
						// Odd rows are shifted right by 1px
						var dx = uv.V & 1;
						var xOffset = pxStride * (2 * x + dx);
						if (x + dx > 0)
						{
							var z = y * stride + xOffset - pxStride;
							var c = actorColor.A == 0 ? terrainColor.Left : actorColor;
							minimapData[z++] = c.R;
							minimapData[z++] = c.G;
							minimapData[z++] = c.B;
							minimapData[z] = c.A;
						}

						if (xOffset < stride)
						{
							var z = y * stride + xOffset;
							var c = actorColor.A == 0 ? terrainColor.Right : actorColor;
							minimapData[z++] = c.R;
							minimapData[z++] = c.G;
							minimapData[z++] = c.B;
							minimapData[z] = c.A;
						}
					}
					else
					{
						var z = y * stride + pxStride * x;
						var c = actorColor.A == 0 ? terrainColor.Left : actorColor;
						minimapData[z++] = c.R;
						minimapData[z++] = c.G;
						minimapData[z++] = c.B;
						minimapData[z] = c.A;
					}
				}
			}

			var png = new Png(minimapData, SpriteFrameType.Rgba32, bitmapWidth, height);
			return png.Save();
		}

		public override void Save(IReadWritePackage toPackage)
		{
			MapFormat = CurrentMapFormat;

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
			var s = root.WriteToString();
			toPackage.Update("map.yaml", Encoding.UTF8.GetBytes(s));
			toPackage.Update("map.bin", SaveBinaryData());
			var textData = Encoding.UTF8.GetBytes(root.WriteToString());
			if (Package != toPackage || !Enumerable.SequenceEqual(textData, Package.GetStream("map.yaml").ReadAllBytes()))
				toPackage.Update("map.yaml", textData);

			var binaryData = SaveBinaryData();
			if (Package != toPackage || !Enumerable.SequenceEqual(binaryData, Package.GetStream("map.bin").ReadAllBytes()))
				toPackage.Update("map.bin", binaryData);

			Package = toPackage;

			// Update UID to match the newly saved data
			Uid = ComputeUID(toPackage, MapFormat);
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
				if (format < SupportedMapFormat)
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

		public bool Contains(CPos cell)
		{
			if (Grid.Type == MapGridType.RectangularIsometric)
			{
				// .ToMPos() returns the same result if the X and Y coordinates
				// are switched. X < Y is invalid in the RectangularIsometric coordinate system,
				// so we pre-filter these to avoid returning the wrong result
				if (cell.X < cell.Y)
					return false;
			}
			else
			{
				// If the mod uses flat & rectangular maps, ToMPos and Contains(MPos) create unnecessary cost.
				// Just check if CPos is within map bounds.
				if (Grid.MaximumTerrainHeight == 0)
					return Bounds.Contains(cell.X, cell.Y);
			}

			return Contains(cell.ToMPos(this));
		}

		public bool Contains(MPos uv)
		{
			// The first check ensures that the cell is within the valid map region, avoiding
			// potential crashes in deeper code.  All CellLayers have the same geometry, and
			// CustomTerrain is convenient.
			return CustomTerrain.Contains(uv) && ContainsAllProjectedCellsCovering(uv);
		}

		bool ContainsAllProjectedCellsCovering(MPos uv)
		{
			// PERF: Checking the bounds directly here is the same as calling Contains((PPos)uv) but saves an allocation
			if (Grid.MaximumTerrainHeight == 0)
				return Bounds.Contains(uv.U, uv.V);

			// PERF: Most cells lie within a region where no matter their height,
			// all possible projected cells would remain in the map area.
			// For these, we can do a fast-path check.
			if (projectionSafeBounds.Contains(uv.U, uv.V))
				return true;

			// Now we need to do a slow-check. Determine the actual projected tiles
			// as they may or may not be in bounds depending on height.
			// If the cell has no valid projection, then we're off the map.
			var projectedCells = ProjectedCellsCovering(uv);
			if (projectedCells.Length == 0)
				return false;

			foreach (var puv in projectedCells)
				if (!Contains(puv))
					return false;

			return true;
		}

		public bool Contains(PPos puv)
		{
			return Bounds.Contains(puv.U, puv.V);
		}

		public WPos CenterOfCell(CPos cell)
		{
			if (Grid.Type == MapGridType.Rectangular)
				return new WPos(1024 * cell.X + 512, 1024 * cell.Y + 512, 0);

			// Convert from isometric cell position (x, y) to world position (u, v):
			// (a) Consider the relationships:
			//  - Center of origin cell is (512, 512)
			//  - +x adds (512, 512) to world pos
			//  - +y adds (-512, 512) to world pos
			// (b) Therefore:
			//  - ax + by adds (a - b) * 512 + 512 to u
			//  - ax + by adds (a + b) * 512 + 512 to v
			// (c) u, v coordinates run diagonally to the cell axes, and we define
			//     1024 as the length projected onto the primary cell axis
			//  - 512 * sqrt(2) = 724
			var z = Height.TryGetValue(cell, out var height) ? 724 * height + Grid.Ramps[Ramp[cell]].CenterHeightOffset : 0;
			return new WPos(724 * (cell.X - cell.Y + 1), 724 * (cell.X + cell.Y + 1), z);
		}

		public WPos CenterOfSubCell(CPos cell, SubCell subCell)
		{
			var index = (int)subCell;
			if (index >= 0 && index < Grid.SubCellOffsets.Length)
			{
				var center = CenterOfCell(cell);
				var offset = Grid.SubCellOffsets[index];
				if (Ramp.TryGetValue(cell, out var ramp) && ramp != 0)
				{
					var r = Grid.Ramps[ramp];
					offset += new WVec(0, 0, r.HeightOffset(offset.X, offset.Y) - r.CenterHeightOffset);
				}

				return center + offset;
			}

			return CenterOfCell(cell);
		}

		public WRot TerrainOrientation(CPos cell)
		{
			if (Ramp.TryGetValue(cell, out var ramp))
				return Grid.Ramps[ramp].Orientation;

			return WRot.None;
		}

		public WVec Offset(CVec delta, int dz)
		{
			if (Grid.Type == MapGridType.Rectangular)
				return new WVec(1024 * delta.X, 1024 * delta.Y, 0);

			return new WVec(724 * (delta.X - delta.Y), 724 * (delta.X + delta.Y), 724 * dz);
		}

		/// <summary>
		/// The size of the map Height step in world units.
		/// </summary>
		/// RectangularIsometric defines 1024 units along the diagonal axis,
		/// giving a half-tile height step of sqrt(2) * 512
		public WDist CellHeightStep => new WDist(Grid.Type == MapGridType.RectangularIsometric ? 724 : 512);

		public CPos CellContaining(WPos pos)
		{
			if (Grid.Type == MapGridType.Rectangular)
				return new CPos(pos.X / 1024, pos.Y / 1024);

			// Convert from world position to isometric cell position:
			// (a) Subtract ([1/2 cell], [1/2 cell]) to move the rotation center to the middle of the corner cell
			// (b) Rotate axes by -pi/4 to align the world axes with the cell axes
			// (c) Apply an offset so that the integer division by [1 cell] rounds in the right direction:
			//      (i) u is always positive, so add [1/2 cell] (which then partially cancels the -[1 cell] term from the rotation)
			//     (ii) v can be negative, so we need to be careful about rounding directions.  We add [1/2 cell] *away from 0* (negative if y > x).
			// (e) Divide by [1 cell] to bring into cell coords.
			// The world axes are rotated relative to the cell axes, so the standard cell size (1024) is increased by a factor of sqrt(2)
			var u = (pos.Y + pos.X - 724) / 1448;
			var v = (pos.Y - pos.X + (pos.Y > pos.X ? 724 : -724)) / 1448;
			return new CPos(u, v);
		}

		public PPos ProjectedCellCovering(WPos pos)
		{
			var projectedPos = pos - new WVec(0, pos.Z, pos.Z);
			return (PPos)CellContaining(projectedPos).ToMPos(Grid.Type);
		}

		public PPos[] ProjectedCellsCovering(MPos uv)
		{
			if (!initializedCellProjection)
				InitializeCellProjection();

			if (!cellProjection.Contains(uv))
				return NoProjectedCells;

			return cellProjection[uv];
		}

		public List<MPos> Unproject(PPos puv)
		{
			var uv = (MPos)puv;

			if (!initializedCellProjection)
				InitializeCellProjection();

			if (!inverseCellProjection.Contains(uv))
				return new List<MPos>();

			return inverseCellProjection[uv];
		}

		public byte ProjectedHeight(PPos puv)
		{
			return projectedHeight[(MPos)puv];
		}

		void UpdateRamp(CPos cell)
		{
			Ramp[cell] = Rules.TerrainInfo.GetTerrainInfo(Tiles[cell]).RampType;
		}

		void InitializeCellProjection()
		{
			if (initializedCellProjection)
				return;

			initializedCellProjection = true;

			cellProjection = new CellLayer<PPos[]>(this);
			inverseCellProjection = new CellLayer<List<MPos>>(this);
			projectedHeight = new CellLayer<byte>(this);

			// Initialize collections
			foreach (var cell in AllCells)
			{
				var uv = cell.ToMPos(Grid.Type);
				cellProjection[uv] = Array.Empty<PPos>();
				inverseCellProjection[uv] = new List<MPos>(1);
			}

			// Initialize projections
			foreach (var cell in AllCells)
				UpdateProjection(cell);
		}

		void UpdateProjection(CPos cell)
		{
			MPos uv;

			if (!initializedCellProjection)
				InitializeCellProjection();

			if (Grid.MaximumTerrainHeight == 0)
			{
				uv = cell.ToMPos(Grid.Type);
				cellProjection[cell] = new[] { (PPos)uv };
				var inverse = inverseCellProjection[uv];
				inverse.Clear();
				inverse.Add(uv);
				CellProjectionChanged?.Invoke(cell);
				return;
			}

			uv = cell.ToMPos(Grid.Type);

			// Remove old reverse projection
			foreach (var puv in cellProjection[uv])
			{
				var temp = (MPos)puv;
				inverseCellProjection[temp].Remove(uv);
				projectedHeight[temp] = ProjectedCellHeightInner(puv);
			}

			var projected = ProjectCellInner(uv);
			cellProjection[uv] = projected;

			foreach (var puv in projected)
			{
				var temp = (MPos)puv;
				inverseCellProjection[temp].Add(uv);

				var height = ProjectedCellHeightInner(puv);
				projectedHeight[temp] = height;

				// Propagate height up cliff faces
				while (true)
				{
					temp = new MPos(temp.U, temp.V - 1);
					if (!inverseCellProjection.Contains(temp) || inverseCellProjection[temp].Count > 0)
						break;

					projectedHeight[temp] = height;
				}
			}

			CellProjectionChanged?.Invoke(cell);
		}

		byte ProjectedCellHeightInner(PPos puv)
		{
			while (inverseCellProjection.Contains((MPos)puv))
			{
				var inverse = inverseCellProjection[(MPos)puv];
				if (inverse.Count > 0)
				{
					// The original games treat the top of cliffs the same way as the bottom
					// This information isn't stored in the map data, so query the offset from the tileset
					var temp = inverse.MaxBy(uv => uv.V);
					return (byte)(Height[temp] - Rules.TerrainInfo.GetTerrainInfo(Tiles[temp]).Height);
				}

				// Try the next cell down if this is a cliff face
				puv = new PPos(puv.U, puv.V + 1);
			}

			return 0;
		}

		PPos[] ProjectCellInner(MPos uv)
		{
			var mapHeight = Height;
			if (!mapHeight.Contains(uv))
				return NoProjectedCells;

			// Any changes to this function should be reflected when setting projectionSafeBounds.
			var height = mapHeight[uv];
			if (height == 0)
				return new[] { (PPos)uv };

			// Odd-height ramps get bumped up a level to the next even height layer
			if ((height & 1) == 1 && Ramp[uv] != 0)
				height += 1;

			var candidates = new List<PPos>();

			// Odd-height level tiles are equally covered by four projected tiles
			if ((height & 1) == 1)
			{
				if ((uv.V & 1) == 1)
					candidates.Add(new PPos(uv.U + 1, uv.V - height));
				else
					candidates.Add(new PPos(uv.U - 1, uv.V - height));

				candidates.Add(new PPos(uv.U, uv.V - height));
				candidates.Add(new PPos(uv.U, uv.V - height + 1));
				candidates.Add(new PPos(uv.U, uv.V - height - 1));
			}
			else
				candidates.Add(new PPos(uv.U, uv.V - height));

			return candidates.Where(c => mapHeight.Contains((MPos)c)).ToArray();
		}

		public TerrainTypeInfo GetTerrainInfo(CPos cell)
		{
			return Rules.TerrainInfo.TerrainTypes[GetTerrainIndex(cell)];
		}

		public CPos Clamp(CPos cell)
		{
			return Clamp(cell.ToMPos(this)).ToCPos(this);
		}

		public MPos Clamp(MPos uv)
		{
			if (Grid.MaximumTerrainHeight == 0)
				return (MPos)Clamp((PPos)uv);

			// Already in bounds, so don't need to do anything.
			if (ContainsAllProjectedCellsCovering(uv))
				return uv;

			// Clamping map coordinates is trickier than it might first look!
			// This needs to handle three nasty cases:
			//  * The requested cell is well outside the map region
			//  * The requested cell is near the top edge inside the map but outside the projected layer
			//  * The clamped projected cell lands on a cliff face with no associated map cell
			//
			// Handling these cases properly requires abuse of our knowledge of the projection transform.
			//
			// The U coordinate doesn't change significantly in the projection, so clamp this
			// straight away and ensure the point is somewhere inside the map
			uv = cellProjection.Clamp(new MPos(uv.U.Clamp(Bounds.Left, Bounds.Right), uv.V));

			// Project this guessed cell and take the first available cell
			// If it is projected outside the layer, then make another guess.
			var allProjected = ProjectedCellsCovering(uv);
			var projected = allProjected.Length > 0 ? allProjected.First()
				: new PPos(uv.U, uv.V.Clamp(Bounds.Top, Bounds.Bottom));

			// Clamp the projected cell to the map area
			projected = Clamp(projected);

			// Project the cell back into map coordinates.
			// This may fail if the projected cell covered a cliff or another feature
			// where there is a large change in terrain height.
			var unProjected = Unproject(projected);
			if (unProjected.Count == 0)
			{
				// Adjust V until we find a cell that works
				for (var x = 2; x <= 2 * Grid.MaximumTerrainHeight; x++)
				{
					var dv = ((x & 1) == 1 ? 1 : -1) * x / 2;
					var test = new PPos(projected.U, projected.V + dv);
					if (!Contains(test))
						continue;

					unProjected = Unproject(test);
					if (unProjected.Count > 0)
						break;
				}

				// This shouldn't happen.  But if it does, return the original value and hope the caller doesn't explode.
				if (unProjected.Count == 0)
				{
					Log.Write("debug", "Failed to clamp map cell {0} to map bounds", uv);
					return uv;
				}
			}

			return projected.V == Bounds.Bottom ? unProjected.MaxBy(x => x.V) : unProjected.MinBy(x => x.V);
		}

		public PPos Clamp(PPos puv)
		{
			var bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width - 1, Bounds.Height - 1);
			return puv.Clamp(bounds);
		}

		public CPos ChooseRandomCell(MersenneTwister rand)
		{
			List<MPos> cells;
			do
			{
				var u = rand.Next(Bounds.Left, Bounds.Right);
				var v = rand.Next(Bounds.Top, Bounds.Bottom);

				cells = Unproject(new PPos(u, v));
			}
			while (cells.Count == 0);

			return cells.Random(rand).ToCPos(Grid.Type);
		}

		public CPos ChooseClosestEdgeCell(CPos cell)
		{
			return ChooseClosestEdgeCell(cell.ToMPos(Grid.Type)).ToCPos(Grid.Type);
		}

		public MPos ChooseClosestEdgeCell(MPos uv)
		{
			var allProjected = ProjectedCellsCovering(uv);

			PPos edge;
			if (allProjected.Length > 0)
			{
				var puv = allProjected.First();
				var horizontalBound = (puv.U - Bounds.Left < Bounds.Width / 2) ? Bounds.Left : Bounds.Right;
				var verticalBound = (puv.V - Bounds.Top < Bounds.Height / 2) ? Bounds.Top : Bounds.Bottom;

				var du = Math.Abs(horizontalBound - puv.U);
				var dv = Math.Abs(verticalBound - puv.V);

				edge = du < dv ? new PPos(horizontalBound, puv.V) : new PPos(puv.U, verticalBound);
			}
			else
				edge = new PPos(Bounds.Left, Bounds.Top);

			var unProjected = Unproject(edge);
			if (unProjected.Count == 0)
			{
				// Adjust V until we find a cell that works
				for (var x = 2; x <= 2 * Grid.MaximumTerrainHeight; x++)
				{
					var dv = ((x & 1) == 1 ? 1 : -1) * x / 2;
					var test = new PPos(edge.U, edge.V + dv);
					if (!Contains(test))
						continue;

					unProjected = Unproject(test);
					if (unProjected.Count > 0)
						break;
				}

				// This shouldn't happen.  But if it does, return the original value and hope the caller doesn't explode.
				if (unProjected.Count == 0)
				{
					Log.Write("debug", "Failed to find closest edge for map cell {0}", uv);
					return uv;
				}
			}

			return edge.V == Bounds.Bottom ? unProjected.MaxBy(x => x.V) : unProjected.MinBy(x => x.V);
		}

		public CPos ChooseClosestMatchingEdgeCell(CPos cell, Func<CPos, bool> match)
		{
			return AllEdgeCells.OrderBy(c => (cell - c).Length).FirstOrDefault(c => match(c));
		}

		List<CPos> UpdateEdgeCells()
		{
			var edgeCells = new List<CPos>();
			var unProjected = new List<MPos>();
			var bottom = Bounds.Bottom - 1;
			for (var u = Bounds.Left; u < Bounds.Right; u++)
			{
				unProjected = Unproject(new PPos(u, Bounds.Top));
				if (unProjected.Count > 0)
					edgeCells.Add(unProjected.MinBy(x => x.V).ToCPos(Grid.Type));

				unProjected = Unproject(new PPos(u, bottom));
				if (unProjected.Count > 0)
					edgeCells.Add(unProjected.MaxBy(x => x.V).ToCPos(Grid.Type));
			}

			for (var v = Bounds.Top; v < Bounds.Bottom; v++)
			{
				unProjected = Unproject(new PPos(Bounds.Left, v));
				if (unProjected.Count > 0)
					edgeCells.Add((v == bottom ? unProjected.MaxBy(x => x.V) : unProjected.MinBy(x => x.V)).ToCPos(Grid.Type));

				unProjected = Unproject(new PPos(Bounds.Right - 1, v));
				if (unProjected.Count > 0)
					edgeCells.Add((v == bottom ? unProjected.MaxBy(x => x.V) : unProjected.MinBy(x => x.V)).ToCPos(Grid.Type));
			}

			return edgeCells;
		}

		protected static readonly PPos[] NoProjectedCells = Array.Empty<PPos>();

		public void Resize(int width, int height)
		{
			var oldMapTiles = Tiles;
			var oldMapResources = Resources;
			var oldMapHeight = Height;
			var oldMapRamp = Ramp;
			var newSize = new Size(width, height);

			Tiles = CellLayer.Resize(oldMapTiles, newSize, oldMapTiles[MPos.Zero]);
			Resources = CellLayer.Resize(oldMapResources, newSize, oldMapResources[MPos.Zero]);
			Height = CellLayer.Resize(oldMapHeight, newSize, oldMapHeight[MPos.Zero]);
			Ramp = CellLayer.Resize(oldMapRamp, newSize, oldMapRamp[MPos.Zero]);
			MapSize = new int2(newSize);

			var tl = new MPos(0, 0);
			var br = new MPos(MapSize.X - 1, MapSize.Y - 1);
			AllCells = new CellRegion(Grid.Type, tl.ToCPos(this), br.ToCPos(this));
			SetBounds(new PPos(tl.U + 1, tl.V + 1), new PPos(br.U - 1, br.V - 1));
		}

		public void NewSize(Size size, ITerrainInfo terrainInfo)
		{
			Tiles = new CellLayer<TerrainTile>(Grid.Type, size);
			Resources = new CellLayer<ResourceTile>(Grid.Type, size);
			Height = new CellLayer<byte>(Grid.Type, size);
			Ramp = new CellLayer<byte>(Grid.Type, size);
			Tiles.Clear(terrainInfo.DefaultTerrainTile);

			if (Grid.MaximumTerrainHeight > 0)
			{
				Tiles.CellEntryChanged += UpdateRamp;
				Tiles.CellEntryChanged += UpdateProjection;
				Height.CellEntryChanged += UpdateProjection;
			}

			var tl = new MPos(0, 0);
			var br = new MPos(MapSize.X - 1, MapSize.Y - 1);
			AllCells = new CellRegion(Grid.Type, tl.ToCPos(this), br.ToCPos(this));
			SetBounds(new PPos(tl.U + 1, tl.V + 1), new PPos(br.U - 1, br.V - 1));
		}

		public void SetBounds(PPos tl, PPos br)
		{
			// The tl and br coordinates are inclusive, but the Rectangle
			// is exclusive.  Pad the right and bottom edges to match.
			Bounds = Rectangle.FromLTRB(tl.U, tl.V, br.U + 1, br.V + 1);

			// See ProjectCellInner to see how any given position may be projected.
			// U: May gain or lose 1, so bring in the left and right edge by 1.
			// V: For an even height tile, this ranges from 0 to height
			//    For an odd tile, the height may get rounded up to next even.
			//    Then also it projects to four tiles which adds one more to the possible height change.
			//    So we get a range of 0 to height + 1 + 1.
			//    As the height only goes upwards, we only need to make room at the top of the map and not the bottom.
			var maxHeight = Grid.MaximumTerrainHeight;
			if ((maxHeight & 1) == 1)
				maxHeight += 2;
			projectionSafeBounds = Rectangle.FromLTRB(
				Bounds.Left + 1,
				Bounds.Top + maxHeight,
				Bounds.Right - 1,
				Bounds.Bottom);

			// Directly calculate the projected map corners in world units avoiding unnecessary
			// conversions.  This abuses the definition that the width of the cell along the x world axis
			// is always 1024 or 1448 units, and that the height of two rows is 2048 for classic cells and 724
			// for isometric cells.
			if (Grid.Type == MapGridType.RectangularIsometric)
			{
				ProjectedTopLeft = new WPos(tl.U * 1448, tl.V * 724, 0);
				ProjectedBottomRight = new WPos(br.U * 1448 - 1, (br.V + 1) * 724 - 1, 0);
			}
			else
			{
				ProjectedTopLeft = new WPos(tl.U * 1024, tl.V * 1024, 0);
				ProjectedBottomRight = new WPos(br.U * 1024 - 1, (br.V + 1) * 1024 - 1, 0);
			}

			// PERF: This enumeration isn't going to change during the game
			ProjectedCells = new ProjectedCellRegion(this, tl, br).ToArray();
		}

		public byte GetTerrainIndex(CPos cell)
		{
			// Lazily initialize a cache for terrain indexes.
			if (cachedTerrainIndexes == null)
			{
				cachedTerrainIndexes = new CellLayer<short>(this);
				cachedTerrainIndexes.Clear(InvalidCachedTerrainIndex);
			}

			var uv = cell.ToMPos(this);
			var terrainIndex = cachedTerrainIndexes[uv];

			// PERF: Cache terrain indexes per cell on demand.
			if (terrainIndex == InvalidCachedTerrainIndex)
			{
				var custom = CustomTerrain[uv];
				terrainIndex = cachedTerrainIndexes[uv] = custom != byte.MaxValue ? custom : Rules.TerrainInfo.GetTerrainInfo(Tiles[uv]).TerrainType;
			}

			return (byte)terrainIndex;
		}

		public CPos ChooseRandomEdgeCell(MersenneTwister rand)
		{
			return AllEdgeCells.Random(rand);
		}

		public WDist DistanceToEdge(WPos pos, in WVec dir)
		{
			var projectedPos = pos - new WVec(0, pos.Z, pos.Z);
			var x = dir.X == 0 ? int.MaxValue : ((dir.X < 0 ? ProjectedTopLeft.X : ProjectedBottomRight.X) - projectedPos.X) / dir.X;
			var y = dir.Y == 0 ? int.MaxValue : ((dir.Y < 0 ? ProjectedTopLeft.Y : ProjectedBottomRight.Y) - projectedPos.Y) / dir.Y;
			return new WDist(Math.Min(x, y) * dir.Length);
		}

		public WAngle FacingBetween(CPos cell, CPos towards, WAngle fallbackfacing)
		{
			var delta = CenterOfCell(towards) - CenterOfCell(cell);
			if (delta.HorizontalLengthSquared == 0)
				return fallbackfacing;

			return delta.Yaw;
		}

		public WDist DistanceAboveTerrain(WPos pos)
		{
			if (Grid.Type == MapGridType.Rectangular)
				return new WDist(pos.Z);

			// Apply ramp offset
			var cell = CellContaining(pos);
			var offset = pos - CenterOfCell(cell);

			if (Ramp.TryGetValue(cell, out var ramp) && ramp != 0)
			{
				var r = Grid.Ramps[ramp];
				return new WDist(offset.Z + r.CenterHeightOffset - r.HeightOffset(offset.X, offset.Y));
			}

			return new WDist(offset.Z);
		}

		// Both ranges are inclusive because everything that calls it is designed for maxRange being inclusive:
		// it rounds the actual distance up to the next integer so that this call
		// will return any cells that intersect with the requested range circle.
		// The returned positions are sorted by distance from the center.
		public IEnumerable<CPos> FindTilesInAnnulus(CPos center, int minRange, int maxRange, bool allowOutsideBounds = false)
		{
			if (maxRange < minRange)
				throw new ArgumentOutOfRangeException(nameof(maxRange), "Maximum range is less than the minimum range.");

			if (maxRange >= GridTilesByDistance.Length)
				throw new ArgumentOutOfRangeException(nameof(maxRange),
					$"The requested range ({maxRange}) cannot exceed the value of MaximumTileSearchRange ({Grid.MaximumTileSearchRange})");

			for (var i = minRange; i <= maxRange; i++)
			{
				foreach (var offset in GridTilesByDistance[i])
				{
					var t = offset + center;
					if (allowOutsideBounds ? Tiles.Contains(t) : Contains(t))
						yield return t;
				}
			}
		}

		public IEnumerable<CPos> FindTilesInCircle(CPos center, int maxRange, bool allowOutsideBounds = false)
		{
			return FindTilesInAnnulus(center, 0, maxRange, allowOutsideBounds);
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

		public void Dispose()
		{
			Sequences.Dispose();
		}
	}
}
