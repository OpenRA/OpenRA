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

namespace OpenRA
{
	struct BinaryDataHeader
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
				throw new InvalidDataException("Unknown binary map format '{0}'".F(Format));
		}
	}

	[Flags]
	public enum MapVisibility
	{
		Lobby = 1,
		Shellmap = 2,
		MissionSelector = 4
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

			field = typeof(Map).GetField(this.fieldName);
			property = typeof(Map).GetProperty(this.fieldName);
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
					throw new YamlException("Required field `{0}` not found in map.yaml".F(key));
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
				if (required || listValue.Any())
					nodes.Add(new MiniYamlNode(key, null, listValue));
			}
			else if (type == Type.MiniYaml)
			{
				var yamlValue = (MiniYaml)value;
				if (required || (yamlValue != null && (yamlValue.Value != null || yamlValue.Nodes.Any())))
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

	public class Map : IReadOnlyFileSystem
	{
		public const int SupportedMapFormat = 11;

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
		public readonly byte TileFormat = 2;

		// Standard yaml metadata
		public string RequiresMod;
		public string Title;
		public string Author;
		public string Tileset;
		public bool LockPreview;
		public Rectangle Bounds;
		public MapVisibility Visibility = MapVisibility.Lobby;
		public string[] Categories = { "Conquest" };

		public int2 MapSize { get; private set; }

		// Player and actor yaml. Public for access by the map importers and lint checks.
		public List<MiniYamlNode> PlayerDefinitions = new List<MiniYamlNode>();
		public List<MiniYamlNode> ActorDefinitions = new List<MiniYamlNode>();

		// Custom map yaml. Public for access by the map importers and lint checks
		public readonly MiniYaml RuleDefinitions;
		public readonly MiniYaml SequenceDefinitions;
		public readonly MiniYaml ModelSequenceDefinitions;
		public readonly MiniYaml WeaponDefinitions;
		public readonly MiniYaml VoiceDefinitions;
		public readonly MiniYaml MusicDefinitions;
		public readonly MiniYaml NotificationDefinitions;

		public readonly Dictionary<CPos, TerrainTile> ReplacedInvalidTerrainTiles = new Dictionary<CPos, TerrainTile>();

		// Generated data
		public readonly MapGrid Grid;
		public IReadOnlyPackage Package { get; private set; }
		public string Uid { get; private set; }

		public Ruleset Rules { get; private set; }
		public bool InvalidCustomRules { get; private set; }
		public Exception InvalidCustomRulesException { get; private set; }

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

		public CellLayer<TerrainTile> Tiles { get; private set; }
		public CellLayer<ResourceTile> Resources { get; private set; }
		public CellLayer<byte> Height { get; private set; }
		public CellLayer<byte> Ramp { get; private set; }
		public CellLayer<byte> CustomTerrain { get; private set; }

		public PPos[] ProjectedCells { get; private set; }
		public CellRegion AllCells { get; private set; }
		public List<CPos> AllEdgeCells { get; private set; }

		// Internal data
		readonly ModData modData;
		CellLayer<short> cachedTerrainIndexes;
		bool initializedCellProjection;
		CellLayer<PPos[]> cellProjection;
		CellLayer<List<MPos>> inverseCellProjection;
		CellLayer<byte> projectedHeight;

		public static string ComputeUID(IReadOnlyPackage package)
		{
			// UID is calculated by taking an SHA1 of the yaml and binary data
			var requiredFiles = new[] { "map.yaml", "map.bin" };
			var contents = package.Contents.ToList();
			foreach (var required in requiredFiles)
				if (!contents.Contains(required))
					throw new FileNotFoundException("Required file {0} not present in this map".F(required));

			var streams = new List<Stream>();
			try
			{
				foreach (var filename in contents)
					if (filename.EndsWith(".yaml") || filename.EndsWith(".bin") || filename.EndsWith(".lua"))
						streams.Add(package.GetStream(filename));

				// Take the SHA1
				if (streams.Count == 0)
					return CryptoUtil.SHA1Hash(new byte[0]);

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
		public Map(ModData modData, TileSet tileset, int width, int height)
		{
			this.modData = modData;
			var size = new Size(width, height);
			Grid = modData.Manifest.Get<MapGrid>();

			Title = "Name your map here";
			Author = "Your name here";

			MapSize = new int2(size);
			Tileset = tileset.Id;

			// Empty rules that can be added to by the importers.
			// Will be dropped on save if nothing is added to it
			RuleDefinitions = new MiniYaml("");

			Tiles = new CellLayer<TerrainTile>(Grid.Type, size);
			Resources = new CellLayer<ResourceTile>(Grid.Type, size);
			Height = new CellLayer<byte>(Grid.Type, size);
			Ramp = new CellLayer<byte>(Grid.Type, size);
			if (Grid.MaximumTerrainHeight > 0)
			{
				Height.CellEntryChanged += UpdateProjection;
				Tiles.CellEntryChanged += UpdateProjection;
				Tiles.CellEntryChanged += UpdateRamp;
			}

			Tiles.Clear(tileset.DefaultTerrainTile);

			PostInit();
		}

		public Map(ModData modData, IReadOnlyPackage package)
		{
			this.modData = modData;
			Package = package;

			if (!Package.Contains("map.yaml") || !Package.Contains("map.bin"))
				throw new InvalidDataException("Not a valid map\n File: {0}".F(package.Name));

			var yaml = new MiniYaml(null, MiniYaml.FromStream(Package.GetStream("map.yaml"), package.Name));
			foreach (var field in YamlFields)
				field.Deserialize(this, yaml.Nodes);

			if (MapFormat != SupportedMapFormat)
				throw new InvalidDataException("Map format {0} is not supported.\n File: {1}".F(MapFormat, package.Name));

			PlayerDefinitions = MiniYaml.NodesOrEmpty(yaml, "Players");
			if (PlayerDefinitions.Count > 64)
				throw new InvalidDataException("Maps must not define more than 64 players.\n File: {0}".F(package.Name));

			ActorDefinitions = MiniYaml.NodesOrEmpty(yaml, "Actors");

			Grid = modData.Manifest.Get<MapGrid>();

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
								index = (byte)(i % 4 + (j % 4) * 4);

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

			if (Grid.MaximumTerrainHeight > 0)
			{
				Tiles.CellEntryChanged += UpdateRamp;
				Tiles.CellEntryChanged += UpdateProjection;
				Height.CellEntryChanged += UpdateProjection;
			}

			PostInit();

			Uid = ComputeUID(Package);
		}

		void PostInit()
		{
			try
			{
				Rules = Ruleset.Load(modData, this, Tileset, RuleDefinitions, WeaponDefinitions,
					VoiceDefinitions, NotificationDefinitions, MusicDefinitions, SequenceDefinitions, ModelSequenceDefinitions);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to load rules for {0} with error {1}", Title, e);
				InvalidCustomRules = true;
				InvalidCustomRulesException = e;
				Rules = Ruleset.LoadDefaultsForTileSet(modData, Tileset);
			}

			Rules.Sequences.Preload();

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
			var tileset = Rules.TileSet;
			foreach (var uv in AllCells.MapCoords)
			{
				if (!tileset.TryGetTileInfo(Tiles[uv], out var info))
				{
					ReplacedInvalidTerrainTiles[uv.ToCPos(this)] = Tiles[uv];
					Tiles[uv] = tileset.DefaultTerrainTile;
					info = tileset.GetTileInfo(tileset.DefaultTerrainTile);
				}

				Ramp[uv] = info.RampType;
			}

			AllEdgeCells = UpdateEdgeCells();
		}

		void UpdateRamp(CPos cell)
		{
			Ramp[cell] = Rules.TileSet.GetTileInfo(Tiles[cell]).RampType;
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
				cellProjection[uv] = new PPos[0];
				inverseCellProjection[uv] = new List<MPos>(1);
			}

			// Initialize projections
			foreach (var cell in AllCells)
				UpdateProjection(cell);
		}

		void UpdateProjection(CPos cell)
		{
			MPos uv;

			if (Grid.MaximumTerrainHeight == 0)
			{
				uv = cell.ToMPos(Grid.Type);
				cellProjection[cell] = new[] { (PPos)uv };
				var inverse = inverseCellProjection[uv];
				inverse.Clear();
				inverse.Add(uv);
				return;
			}

			if (!initializedCellProjection)
				InitializeCellProjection();

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
					if (!inverseCellProjection.Contains(temp) || inverseCellProjection[temp].Any())
						break;

					projectedHeight[temp] = height;
				}
			}
		}

		byte ProjectedCellHeightInner(PPos puv)
		{
			while (inverseCellProjection.Contains((MPos)puv))
			{
				var inverse = inverseCellProjection[(MPos)puv];
				if (inverse.Any())
				{
					// The original games treat the top of cliffs the same way as the bottom
					// This information isn't stored in the map data, so query the offset from the tileset
					var temp = inverse.MaxBy(uv => uv.V);
					var terrain = Tiles[temp];
					return (byte)(Height[temp] - Rules.TileSet.Templates[terrain.Type][terrain.Index].Height);
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

		public void Save(IReadWritePackage toPackage)
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
				toPackage.Update("map.png", SavePreview());

			// Update the package with the new map data
			var s = root.WriteToString();
			toPackage.Update("map.yaml", Encoding.UTF8.GetBytes(s));
			toPackage.Update("map.bin", SaveBinaryData());
			Package = toPackage;

			// Update UID to match the newly saved data
			Uid = ComputeUID(toPackage);
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

		public (Color Left, Color Right) GetTerrainColorPair(MPos uv)
		{
			Color left, right;
			var tileset = Rules.TileSet;
			var type = tileset.GetTileInfo(Tiles[uv]);
			if (type.MinColor != type.MaxColor)
			{
				left = Exts.ColorLerp(Game.CosmeticRandom.NextFloat(), type.MinColor, type.MaxColor);
				right = Exts.ColorLerp(Game.CosmeticRandom.NextFloat(), type.MinColor, type.MaxColor);
			}
			else
				left = right = type.MinColor;

			if (tileset.MinHeightColorBrightness != 1.0f || tileset.MaxHeightColorBrightness != 1.0f)
			{
				var scale = float2.Lerp(tileset.MinHeightColorBrightness, tileset.MaxHeightColorBrightness, Height[uv] * 1f / Grid.MaximumTerrainHeight);
				left = Color.FromArgb((int)(scale * left.R).Clamp(0, 255), (int)(scale * left.G).Clamp(0, 255), (int)(scale * left.B).Clamp(0, 255));
				right = Color.FromArgb((int)(scale * right.R).Clamp(0, 255), (int)(scale * right.G).Clamp(0, 255), (int)(scale * right.B).Clamp(0, 255));
			}

			return (left, right);
		}

		public byte[] SavePreview()
		{
			var actorTypes = Rules.Actors.Values.Where(a => a.HasTraitInfo<IMapPreviewSignatureInfo>());
			var actors = ActorDefinitions.Where(a => actorTypes.Where(ai => ai.Name == a.Value.Value).Any());
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
			var worldActorInfo = Rules.Actors["world"];
			var worldimpsis = worldActorInfo.TraitInfos<IMapPreviewSignatureInfo>();
			foreach (var worldimpsi in worldimpsis)
				worldimpsi.PopulateMapPreviewSignatureCells(this, worldActorInfo, null, positions);

			var isRectangularIsometric = Grid.Type == MapGridType.RectangularIsometric;

			// Fudge the heightmap offset by adding as much extra as we need / can.
			// This tries to correct for our incorrect assumption that MPos == PPos
			var heightOffset = Math.Min(Grid.MaximumTerrainHeight, MapSize.Y - Bounds.Bottom);
			var width = Bounds.Width;
			var height = Bounds.Height + heightOffset;

			var bitmapWidth = width;
			if (isRectangularIsometric)
				bitmapWidth = 2 * bitmapWidth - 1;

			var stride = bitmapWidth * 4;
			var pxStride = 4;
			var minimapData = new byte[stride * height];
			(Color Left, Color Right) terrainColor = default((Color, Color));

			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var uv = new MPos(x + Bounds.Left, y + Bounds.Top);

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

		public bool Contains(CPos cell)
		{
			// .ToMPos() returns the same result if the X and Y coordinates
			// are switched. X < Y is invalid in the RectangularIsometric coordinate system,
			// so we pre-filter these to avoid returning the wrong result
			if (Grid.Type == MapGridType.RectangularIsometric && cell.X < cell.Y)
				return false;

			// If the mod uses flat & rectangular maps, ToMPos and Contains(MPos) create unnecessary cost.
			// Just check if CPos is within map bounds.
			if (Grid.MaximumTerrainHeight == 0 && Grid.Type == MapGridType.Rectangular)
				return Bounds.Contains(cell.X, cell.Y);

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
			var z = Height.Contains(cell) ? 724 * Height[cell] + Grid.Ramps[Ramp[cell]].CenterHeightOffset : 0;
			return new WPos(724 * (cell.X - cell.Y + 1), 724 * (cell.X + cell.Y + 1), z);
		}

		public WPos CenterOfSubCell(CPos cell, SubCell subCell)
		{
			var index = (int)subCell;
			if (index >= 0 && index < Grid.SubCellOffsets.Length)
			{
				var center = CenterOfCell(cell);
				var offset = Grid.SubCellOffsets[index];
				var ramp = Ramp.Contains(cell) ? Ramp[cell] : 0;
				if (ramp != 0)
				{
					var r = Grid.Ramps[ramp];
					offset += new WVec(0, 0, r.HeightOffset(offset.X, offset.Y) - r.CenterHeightOffset);
				}

				return center + offset;
			}

			return CenterOfCell(cell);
		}

		public WDist DistanceAboveTerrain(WPos pos)
		{
			if (Grid.Type == MapGridType.Rectangular)
				return new WDist(pos.Z);

			// Apply ramp offset
			var cell = CellContaining(pos);
			var offset = pos - CenterOfCell(cell);

			if (!Ramp.Contains(cell))
				return new WDist(offset.Z);

			var ramp = Ramp[cell];
			if (ramp != 0)
			{
				var r = Grid.Ramps[ramp];
				return new WDist(offset.Z + r.CenterHeightOffset - r.HeightOffset(offset.X, offset.Y));
			}

			return new WDist(offset.Z);
		}

		public WVec Offset(CVec delta, int dz)
		{
			if (Grid.Type == MapGridType.Rectangular)
				return new WVec(1024 * delta.X, 1024 * delta.Y, 0);

			return new WVec(724 * (delta.X - delta.Y), 724 * (delta.X + delta.Y), 724 * dz);
		}

		/// <summary>
		/// The size of the map Height step in world units
		/// </summary>
		public WDist CellHeightStep
		{
			get
			{
				// RectangularIsometric defines 1024 units along the diagonal axis,
				// giving a half-tile height step of sqrt(2) * 512
				return new WDist(Grid.Type == MapGridType.RectangularIsometric ? 724 : 512);
			}
		}

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

		static readonly PPos[] NoProjectedCells = { };
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

		public WAngle FacingBetween(CPos cell, CPos towards, WAngle fallbackfacing)
		{
			var delta = CenterOfCell(towards) - CenterOfCell(cell);
			if (delta.HorizontalLengthSquared == 0)
				return fallbackfacing;

			return delta.Yaw;
		}

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

		public void SetBounds(PPos tl, PPos br)
		{
			// The tl and br coordinates are inclusive, but the Rectangle
			// is exclusive.  Pad the right and bottom edges to match.
			Bounds = Rectangle.FromLTRB(tl.U, tl.V, br.U + 1, br.V + 1);

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

		public void FixOpenAreas()
		{
			var r = new Random();
			var tileset = Rules.TileSet;

			for (var j = Bounds.Top; j < Bounds.Bottom; j++)
			{
				for (var i = Bounds.Left; i < Bounds.Right; i++)
				{
					var type = Tiles[new MPos(i, j)].Type;
					var index = Tiles[new MPos(i, j)].Index;
					if (!tileset.Templates.ContainsKey(type))
					{
						Console.WriteLine("Unknown Tile ID {0}".F(type));
						continue;
					}

					var template = tileset.Templates[type];
					if (!template.PickAny)
						continue;

					index = (byte)r.Next(0, template.TilesCount);
					Tiles[new MPos(i, j)] = new TerrainTile(type, index);
				}
			}
		}

		public byte GetTerrainIndex(CPos cell)
		{
			const short InvalidCachedTerrainIndex = -1;

			// Lazily initialize a cache for terrain indexes.
			if (cachedTerrainIndexes == null)
			{
				cachedTerrainIndexes = new CellLayer<short>(this);
				cachedTerrainIndexes.Clear(InvalidCachedTerrainIndex);

				// Invalidate the entry for a cell if anything could cause the terrain index to change.
				Action<CPos> invalidateTerrainIndex = c => cachedTerrainIndexes[c] = InvalidCachedTerrainIndex;
				CustomTerrain.CellEntryChanged += invalidateTerrainIndex;
				Tiles.CellEntryChanged += invalidateTerrainIndex;
			}

			var uv = cell.ToMPos(this);
			var terrainIndex = cachedTerrainIndexes[uv];

			// PERF: Cache terrain indexes per cell on demand.
			if (terrainIndex == InvalidCachedTerrainIndex)
			{
				var custom = CustomTerrain[uv];
				terrainIndex = cachedTerrainIndexes[uv] =
					custom != byte.MaxValue ? custom : Rules.TileSet.GetTerrainIndex(Tiles[uv]);
			}

			return (byte)terrainIndex;
		}

		public TerrainTypeInfo GetTerrainInfo(CPos cell)
		{
			return Rules.TileSet[GetTerrainIndex(cell)];
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
			var projected = allProjected.Any() ? allProjected.First()
				: new PPos(uv.U, uv.V.Clamp(Bounds.Top, Bounds.Bottom));

			// Clamp the projected cell to the map area
			projected = Clamp(projected);

			// Project the cell back into map coordinates.
			// This may fail if the projected cell covered a cliff or another feature
			// where there is a large change in terrain height.
			var unProjected = Unproject(projected);
			if (!unProjected.Any())
			{
				// Adjust V until we find a cell that works
				for (var x = 2; x <= 2 * Grid.MaximumTerrainHeight; x++)
				{
					var dv = ((x & 1) == 1 ? 1 : -1) * x / 2;
					var test = new PPos(projected.U, projected.V + dv);
					if (!Contains(test))
						continue;

					unProjected = Unproject(test);
					if (unProjected.Any())
						break;
				}

				// This shouldn't happen.  But if it does, return the original value and hope the caller doesn't explode.
				if (!unProjected.Any())
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
			while (!cells.Any());

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
			if (allProjected.Any())
			{
				var puv = allProjected.First();
				var horizontalBound = ((puv.U - Bounds.Left) < Bounds.Width / 2) ? Bounds.Left : Bounds.Right;
				var verticalBound = ((puv.V - Bounds.Top) < Bounds.Height / 2) ? Bounds.Top : Bounds.Bottom;

				var du = Math.Abs(horizontalBound - puv.U);
				var dv = Math.Abs(verticalBound - puv.V);

				edge = du < dv ? new PPos(horizontalBound, puv.V) : new PPos(puv.U, verticalBound);
			}
			else
				edge = new PPos(Bounds.Left, Bounds.Top);

			var unProjected = Unproject(edge);
			if (!unProjected.Any())
			{
				// Adjust V until we find a cell that works
				for (var x = 2; x <= 2 * Grid.MaximumTerrainHeight; x++)
				{
					var dv = ((x & 1) == 1 ? 1 : -1) * x / 2;
					var test = new PPos(edge.U, edge.V + dv);
					if (!Contains(test))
						continue;

					unProjected = Unproject(test);
					if (unProjected.Any())
						break;
				}

				// This shouldn't happen.  But if it does, return the original value and hope the caller doesn't explode.
				if (!unProjected.Any())
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
				if (unProjected.Any())
					edgeCells.Add(unProjected.MinBy(x => x.V).ToCPos(Grid.Type));

				unProjected = Unproject(new PPos(u, bottom));
				if (unProjected.Any())
					edgeCells.Add(unProjected.MaxBy(x => x.V).ToCPos(Grid.Type));
			}

			for (var v = Bounds.Top; v < Bounds.Bottom; v++)
			{
				unProjected = Unproject(new PPos(Bounds.Left, v));
				if (unProjected.Any())
					edgeCells.Add((v == bottom ? unProjected.MaxBy(x => x.V) : unProjected.MinBy(x => x.V)).ToCPos(Grid.Type));

				unProjected = Unproject(new PPos(Bounds.Right - 1, v));
				if (unProjected.Any())
					edgeCells.Add((v == bottom ? unProjected.MaxBy(x => x.V) : unProjected.MinBy(x => x.V)).ToCPos(Grid.Type));
			}

			return edgeCells;
		}

		public CPos ChooseRandomEdgeCell(MersenneTwister rand)
		{
			return AllEdgeCells.Random(rand);
		}

		public WDist DistanceToEdge(WPos pos, WVec dir)
		{
			var projectedPos = pos - new WVec(0, pos.Z, pos.Z);
			var x = dir.X == 0 ? int.MaxValue : ((dir.X < 0 ? ProjectedTopLeft.X : ProjectedBottomRight.X) - projectedPos.X) / dir.X;
			var y = dir.Y == 0 ? int.MaxValue : ((dir.Y < 0 ? ProjectedTopLeft.Y : ProjectedBottomRight.Y) - projectedPos.Y) / dir.Y;
			return new WDist(Math.Min(x, y) * dir.Length);
		}

		// Both ranges are inclusive because everything that calls it is designed for maxRange being inclusive:
		// it rounds the actual distance up to the next integer so that this call
		// will return any cells that intersect with the requested range circle.
		// The returned positions are sorted by distance from the center.
		public IEnumerable<CPos> FindTilesInAnnulus(CPos center, int minRange, int maxRange, bool allowOutsideBounds = false)
		{
			if (maxRange < minRange)
				throw new ArgumentOutOfRangeException("maxRange", "Maximum range is less than the minimum range.");

			if (maxRange >= Grid.TilesByDistance.Length)
				throw new ArgumentOutOfRangeException("maxRange",
					"The requested range ({0}) cannot exceed the value of MaximumTileSearchRange ({1})".F(maxRange, Grid.MaximumTileSearchRange));

			for (var i = minRange; i <= maxRange; i++)
			{
				foreach (var offset in Grid.TilesByDistance[i])
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

		public Stream Open(string filename)
		{
			// Explicit package paths never refer to a map
			if (!filename.Contains("|") && Package.Contains(filename))
				return Package.GetStream(filename);

			return modData.DefaultFileSystem.Open(filename);
		}

		public bool TryGetPackageContaining(string path, out IReadOnlyPackage package, out string filename)
		{
			// Packages aren't supported inside maps
			return modData.DefaultFileSystem.TryGetPackageContaining(path, out package, out filename);
		}

		public bool TryOpen(string filename, out Stream s)
		{
			// Explicit package paths never refer to a map
			if (!filename.Contains("|"))
			{
				s = Package.GetStream(filename);
				if (s != null)
					return true;
			}

			return modData.DefaultFileSystem.TryOpen(filename, out s);
		}

		public bool Exists(string filename)
		{
			// Explicit package paths never refer to a map
			if (!filename.Contains("|") && Package.Contains(filename))
				return true;

			return modData.DefaultFileSystem.Exists(filename);
		}

		public bool IsExternalModFile(string filename)
		{
			// Explicit package paths never refer to a map
			if (filename.Contains("|"))
				return modData.DefaultFileSystem.IsExternalModFile(filename);

			return false;
		}
	}
}
