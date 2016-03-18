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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Network;
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

	public class Map : IReadOnlyFileSystem
	{
		public const int SupportedMapFormat = 10;

		public const int MaxTilesInCircleRange = 50;
		public readonly MapGrid Grid;
		readonly ModData modData;

		public IReadOnlyPackage Package { get; private set; }

		// Yaml map data
		public string Uid { get; private set; }
		public int MapFormat;
		public MapVisibility Visibility = MapVisibility.Lobby;
		public string RequiresMod;

		public string Title;
		public string Type = "Conquest";
		public string Author;
		public string Tileset;
		public bool LockPreview;
		public bool InvalidCustomRules { get; private set; }

		public static string ComputeUID(IReadOnlyPackage package)
		{
			// UID is calculated by taking an SHA1 of the yaml and binary data
			var requiredFiles = new[] { "map.yaml", "map.bin" };
			var contents = package.Contents.ToList();
			foreach (var required in requiredFiles)
				if (!contents.Contains(required))
					throw new FileNotFoundException("Required file {0} not present in this map".F(required));

			using (var ms = new MemoryStream())
			{
				foreach (var filename in contents)
					if (filename.EndsWith(".yaml") || filename.EndsWith(".bin") || filename.EndsWith(".lua"))
						using (var s = package.GetStream(filename))
							s.CopyTo(ms);

				// Take the SHA1
				ms.Seek(0, SeekOrigin.Begin);
				using (var csp = SHA1.Create())
					return new string(csp.ComputeHash(ms).SelectMany(a => a.ToString("x2")).ToArray());
			}
		}

		public Rectangle Bounds;

		/// <summary>
		/// The top-left of the playable area in projected world coordinates
		/// This is a hacky workaround for legacy functionality.  Do not use for new code.
		/// </summary>
		public WPos ProjectedTopLeft;

		/// <summary>
		/// The bottom-right of the playable area in projected world coordinates
		/// This is a hacky workaround for legacy functionality.  Do not use for new code.
		/// </summary>
		public WPos ProjectedBottomRight;

		// Yaml map data
		[FieldLoader.Ignore] public readonly MiniYaml RuleDefinitions;
		[FieldLoader.Ignore] public readonly MiniYaml SequenceDefinitions;
		[FieldLoader.Ignore] public readonly MiniYaml VoxelSequenceDefinitions;
		[FieldLoader.Ignore] public readonly MiniYaml WeaponDefinitions;
		[FieldLoader.Ignore] public readonly MiniYaml VoiceDefinitions;
		[FieldLoader.Ignore] public readonly MiniYaml MusicDefinitions;
		[FieldLoader.Ignore] public readonly MiniYaml NotificationDefinitions;
		[FieldLoader.Ignore] public readonly MiniYaml TranslationDefinitions;

		[FieldLoader.Ignore] public List<MiniYamlNode> PlayerDefinitions = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> ActorDefinitions = new List<MiniYamlNode>();

		// Binary map data
		[FieldLoader.Ignore] public byte TileFormat = 2;

		public int2 MapSize;

		[FieldLoader.Ignore] public CellLayer<TerrainTile> Tiles;
		[FieldLoader.Ignore] public CellLayer<ResourceTile> Resources;
		[FieldLoader.Ignore] public CellLayer<byte> Height;

		[FieldLoader.Ignore] public CellLayer<byte> CustomTerrain;
		[FieldLoader.Ignore] CellLayer<short> cachedTerrainIndexes;

		[FieldLoader.Ignore] bool initializedCellProjection;
		[FieldLoader.Ignore] CellLayer<PPos[]> cellProjection;
		[FieldLoader.Ignore] CellLayer<List<MPos>> inverseCellProjection;

		[FieldLoader.Ignore] Lazy<Ruleset> rules;
		public Ruleset Rules { get { return rules != null ? rules.Value : null; } }

		[FieldLoader.Ignore] public ProjectedCellRegion ProjectedCellBounds;
		[FieldLoader.Ignore] public CellRegion AllCells;
		public List<CPos> AllEdgeCells { get; private set; }

		void AssertExists(string filename)
		{
			using (var s = Package.GetStream(filename))
				if (s == null)
					throw new InvalidOperationException("Required file {0} not present in this map".F(filename));
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
			var tileRef = new TerrainTile(tileset.Templates.First().Key, 0);

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
			if (Grid.MaximumTerrainHeight > 0)
			{
				Height.CellEntryChanged += UpdateProjection;
				Tiles.CellEntryChanged += UpdateProjection;
			}

			Tiles.Clear(tileRef);

			PostInit();
		}

		public Map(ModData modData, IReadOnlyPackage package)
		{
			this.modData = modData;
			Package = package;

			AssertExists("map.yaml");
			AssertExists("map.bin");

			var yaml = new MiniYaml(null, MiniYaml.FromStream(Package.GetStream("map.yaml"), package.Name));
			FieldLoader.Load(this, yaml);

			if (MapFormat != SupportedMapFormat)
				throw new InvalidDataException("Map format {0} is not supported.\n File: {1}".F(MapFormat, package.Name));

			RuleDefinitions = LoadRuleSection(yaml, "Rules");
			SequenceDefinitions = LoadRuleSection(yaml, "Sequences");
			VoxelSequenceDefinitions = LoadRuleSection(yaml, "VoxelSequences");
			WeaponDefinitions = LoadRuleSection(yaml, "Weapons");
			VoiceDefinitions = LoadRuleSection(yaml, "Voices");
			MusicDefinitions = LoadRuleSection(yaml, "Music");
			NotificationDefinitions = LoadRuleSection(yaml, "Notifications");
			TranslationDefinitions = LoadRuleSection(yaml, "Translations");

			PlayerDefinitions = MiniYaml.NodesOrEmpty(yaml, "Players");
			ActorDefinitions = MiniYaml.NodesOrEmpty(yaml, "Actors");

			Grid = modData.Manifest.Get<MapGrid>();

			var size = new Size(MapSize.X, MapSize.Y);
			Tiles = new CellLayer<TerrainTile>(Grid.Type, size);
			Resources = new CellLayer<ResourceTile>(Grid.Type, size);
			Height = new CellLayer<byte>(Grid.Type, size);

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
				Tiles.CellEntryChanged += UpdateProjection;
				Height.CellEntryChanged += UpdateProjection;
			}

			PostInit();

			Uid = ComputeUID(Package);
		}

		MiniYaml LoadRuleSection(MiniYaml yaml, string section)
		{
			var node = yaml.Nodes.FirstOrDefault(n => n.Key == section);
			return node != null ? node.Value : null;
		}

		void PostInit()
		{
			rules = Exts.Lazy(() =>
			{
				try
				{
					return Ruleset.Load(modData, this, Tileset, RuleDefinitions, WeaponDefinitions,
						VoiceDefinitions, NotificationDefinitions, MusicDefinitions, SequenceDefinitions);
				}
				catch (Exception e)
				{
					InvalidCustomRules = true;
					Log.Write("debug", "Failed to load rules for {0} with error {1}", Title, e.Message);
				}

				return Ruleset.LoadDefaultsForTileSet(modData, Tileset);
			});

			var tl = new MPos(0, 0).ToCPos(this);
			var br = new MPos(MapSize.X - 1, MapSize.Y - 1).ToCPos(this);
			AllCells = new CellRegion(Grid.Type, tl, br);

			var btl = new PPos(Bounds.Left, Bounds.Top);
			var bbr = new PPos(Bounds.Right - 1, Bounds.Bottom - 1);
			SetBounds(btl, bbr);

			CustomTerrain = new CellLayer<byte>(this);
			foreach (var uv in AllCells.MapCoords)
				CustomTerrain[uv] = byte.MaxValue;

			AllEdgeCells = UpdateEdgeCells();
		}

		void InitializeCellProjection()
		{
			if (initializedCellProjection)
				return;

			initializedCellProjection = true;

			cellProjection = new CellLayer<PPos[]>(this);
			inverseCellProjection = new CellLayer<List<MPos>>(this);

			// Initialize collections
			foreach (var cell in AllCells)
			{
				var uv = cell.ToMPos(Grid.Type);
				cellProjection[uv] = new PPos[0];
				inverseCellProjection[uv] = new List<MPos>();
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
				inverseCellProjection[(MPos)puv].Remove(uv);

			var projected = ProjectCellInner(uv);
			cellProjection[uv] = projected;

			foreach (var puv in projected)
				inverseCellProjection[(MPos)puv].Add(uv);
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
			if ((height & 1) == 1)
			{
				var ti = Rules.TileSet.GetTileInfo(Tiles[uv]);
				if (ti != null && ti.RampType != 0)
					height += 1;
			}

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

		public Ruleset PreloadRules()
		{
			return rules.Value;
		}

		public void Save(IReadWritePackage toPackage)
		{
			MapFormat = SupportedMapFormat;

			var root = new List<MiniYamlNode>();
			var fields = new[]
			{
				"MapFormat",
				"RequiresMod",
				"Title",
				"Author",
				"Tileset",
				"MapSize",
				"Bounds",
				"Visibility",
				"Type",
			};

			foreach (var field in fields)
			{
				var f = GetType().GetField(field);
				if (f.GetValue(this) == null)
					continue;
				root.Add(new MiniYamlNode(field, FieldSaver.FormatValue(this, f)));
			}

			// Save LockPreview field only if it's set
			if (LockPreview)
				root.Add(new MiniYamlNode("LockPreview", "True"));

			root.Add(new MiniYamlNode("Players", null, PlayerDefinitions));
			root.Add(new MiniYamlNode("Actors", null, ActorDefinitions));

			var ruleSections = new[]
			{
				new MiniYamlNode("Rules", RuleDefinitions),
				new MiniYamlNode("Sequences", SequenceDefinitions),
				new MiniYamlNode("VoxelSequences", VoxelSequenceDefinitions),
				new MiniYamlNode("Weapons", WeaponDefinitions),
				new MiniYamlNode("Voices", VoiceDefinitions),
				new MiniYamlNode("Music", MusicDefinitions),
				new MiniYamlNode("Notifications", NotificationDefinitions),
				new MiniYamlNode("Translations", TranslationDefinitions)
			};

			foreach (var section in ruleSections)
				if (section.Value != null && (section.Value.Value != null || section.Value.Nodes.Any()))
					root.Add(section);

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

		public byte[] SavePreview()
		{
			var tileset = Rules.TileSet;
			var resources = Rules.Actors["world"].TraitInfos<ResourceTypeInfo>()
				.ToDictionary(r => r.ResourceType, r => r.TerrainType);

			using (var stream = new MemoryStream())
			{
				var isRectangularIsometric = Grid.Type == MapGridType.RectangularIsometric;

				// Fudge the heightmap offset by adding as much extra as we need / can.
				// This tries to correct for our incorrect assumption that MPos == PPos
				var heightOffset = Math.Min(Grid.MaximumTerrainHeight, MapSize.Y - Bounds.Bottom);
				var width = Bounds.Width;
				var height = Bounds.Height + heightOffset;

				var bitmapWidth = width;
				if (isRectangularIsometric)
					bitmapWidth = 2 * bitmapWidth - 1;

				using (var bitmap = new Bitmap(bitmapWidth, height))
				{
					var bitmapData = bitmap.LockBits(bitmap.Bounds(),
						ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

					unsafe
					{
						var colors = (int*)bitmapData.Scan0;
						var stride = bitmapData.Stride / 4;
						Color leftColor, rightColor;

						for (var y = 0; y < height; y++)
						{
							for (var x = 0; x < width; x++)
							{
								var uv = new MPos(x + Bounds.Left, y + Bounds.Top);
								var resourceType = Resources[uv].Type;
								if (resourceType != 0)
								{
									// Cell contains resources
									string res;
									if (!resources.TryGetValue(resourceType, out res))
										continue;

									leftColor = rightColor = tileset[tileset.GetTerrainIndex(res)].Color;
								}
								else
								{
									// Cell contains terrain
									var type = tileset.GetTileInfo(Tiles[uv]);
									leftColor = type != null ? type.LeftColor : Color.Black;
									rightColor = type != null ? type.RightColor : Color.Black;
								}

								if (isRectangularIsometric)
								{
									// Odd rows are shifted right by 1px
									var dx = uv.V & 1;
									if (x + dx > 0)
										colors[y * stride + 2 * x + dx - 1] = leftColor.ToArgb();

									if (2 * x + dx < stride)
										colors[y * stride + 2 * x + dx] = rightColor.ToArgb();
								}
								else
									colors[y * stride + x] = leftColor.ToArgb();
							}
						}
					}

					bitmap.UnlockBits(bitmapData);
					bitmap.Save(stream, ImageFormat.Png);
				}

				return stream.ToArray();
			}
		}

		public bool Contains(CPos cell)
		{
			// .ToMPos() returns the same result if the X and Y coordinates
			// are switched. X < Y is invalid in the RectangularIsometric coordinate system,
			// so we pre-filter these to avoid returning the wrong result
			if (Grid.Type == MapGridType.RectangularIsometric && cell.X < cell.Y)
				return false;

			return Contains(cell.ToMPos(this));
		}

		public bool Contains(MPos uv)
		{
			// The first check ensures that the cell is within the valid map region, avoiding
			// potential crashes in deeper code.  All CellLayers have the same geometry, and
			// CustomTerrain is convenient (cellProjection may be null and others are Lazy).
			return CustomTerrain.Contains(uv) && ContainsAllProjectedCellsCovering(uv);
		}

		bool ContainsAllProjectedCellsCovering(MPos uv)
		{
			if (Grid.MaximumTerrainHeight == 0)
				return Contains((PPos)uv);

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
			var z = Height.Contains(cell) ? 512 * Height[cell] : 0;
			return new WPos(512 * (cell.X - cell.Y + 1), 512 * (cell.X + cell.Y + 1), z);
		}

		public WPos CenterOfSubCell(CPos cell, SubCell subCell)
		{
			var index = (int)subCell;
			if (index >= 0 && index <= Grid.SubCellOffsets.Length)
				return CenterOfCell(cell) + Grid.SubCellOffsets[index];
			return CenterOfCell(cell);
		}

		public WDist DistanceAboveTerrain(WPos pos)
		{
			var cell = CellContaining(pos);
			var delta = pos - CenterOfCell(cell);
			return new WDist(delta.Z);
		}

		public CPos CellContaining(WPos pos)
		{
			if (Grid.Type == MapGridType.Rectangular)
				return new CPos(pos.X / 1024, pos.Y / 1024);

			// Convert from world position to isometric cell position:
			// (a) Subtract (512, 512) to move the rotation center to the middle of the corner cell
			// (b) Rotate axes by -pi/4
			// (c) Divide through by sqrt(2) to bring us to an equivalent world pos aligned with u,v axes
			// (d) Apply an offset so that the integer division by 1024 rounds in the right direction:
			//      (i) u is always positive, so add 512 (which then partially cancels the -1024 term from the rotation)
			//     (ii) v can be negative, so we need to be careful about rounding directions.  We add 512 *away from 0* (negative if y > x).
			// (e) Divide by 1024 to bring into cell coords.
			var u = (pos.Y + pos.X - 512) / 1024;
			var v = (pos.Y - pos.X + (pos.Y > pos.X ? 512 : -512)) / 1024;
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

		public int FacingBetween(CPos cell, CPos towards, int fallbackfacing)
		{
			var delta = CenterOfCell(towards) - CenterOfCell(cell);
			if (delta.HorizontalLengthSquared == 0)
				return fallbackfacing;

			return delta.Yaw.Facing;
		}

		public void Resize(int width, int height)
		{
			var oldMapTiles = Tiles;
			var oldMapResources = Resources;
			var oldMapHeight = Height;
			var newSize = new Size(width, height);

			Tiles = CellLayer.Resize(oldMapTiles, newSize, oldMapTiles[MPos.Zero]);
			Resources = CellLayer.Resize(oldMapResources, newSize, oldMapResources[MPos.Zero]);
			Height = CellLayer.Resize(oldMapHeight, newSize, oldMapHeight[MPos.Zero]);
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
			// conversions.  This abuses the definition that the width of the cell is always
			// 1024 units, and that the height of two rows is 2048 for classic cells and 1024
			// for isometric cells.
			var wtop = tl.V * 1024;
			var wbottom = (br.V + 1) * 1024;
			if (Grid.Type == MapGridType.RectangularIsometric)
			{
				wtop /= 2;
				wbottom /= 2;
			}

			ProjectedTopLeft = new WPos(tl.U * 1024, wtop, 0);
			ProjectedBottomRight = new WPos(br.U * 1024 - 1, wbottom - 1, 0);

			ProjectedCellBounds = new ProjectedCellRegion(this, tl, br);
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
			} while (!cells.Any());

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

		static readonly CVec[][] TilesByDistance = InitTilesByDistance(MaxTilesInCircleRange);

		static CVec[][] InitTilesByDistance(int max)
		{
			var ts = new List<CVec>[max + 1];
			for (var i = 0; i < max + 1; i++)
				ts[i] = new List<CVec>();

			for (var j = -max; j <= max; j++)
				for (var i = -max; i <= max; i++)
					if (max * max >= i * i + j * j)
						ts[Exts.ISqrt(i * i + j * j, Exts.ISqrtRoundMode.Ceiling)].Add(new CVec(i, j));

			// Sort each integer-distance group by the actual distance
			foreach (var list in ts)
			{
				list.Sort((a, b) =>
				{
					var result = a.LengthSquared.CompareTo(b.LengthSquared);
					if (result != 0)
						return result;

					// If the lengths are equal, use other means to sort them.
					// Try the hash code first because it gives more
					// random-appearing results than X or Y that would always
					// prefer the leftmost/topmost position.
					result = a.GetHashCode().CompareTo(b.GetHashCode());
					if (result != 0)
						return result;

					result = a.X.CompareTo(b.X);
					if (result != 0)
						return result;

					return a.Y.CompareTo(b.Y);
				});
			}

			return ts.Select(list => list.ToArray()).ToArray();
		}

		// Both ranges are inclusive because everything that calls it is designed for maxRange being inclusive:
		// it rounds the actual distance up to the next integer so that this call
		// will return any cells that intersect with the requested range circle.
		// The returned positions are sorted by distance from the center.
		public IEnumerable<CPos> FindTilesInAnnulus(CPos center, int minRange, int maxRange, bool allowOutsideBounds = false)
		{
			if (maxRange < minRange)
				throw new ArgumentOutOfRangeException("maxRange", "Maximum range is less than the minimum range.");

			if (maxRange >= TilesByDistance.Length)
				throw new ArgumentOutOfRangeException("maxRange", "The requested range ({0}) exceeds the maximum allowed ({1})".F(maxRange, MaxTilesInCircleRange));

			Func<CPos, bool> valid = Contains;
			if (allowOutsideBounds)
				valid = Tiles.Contains;

			for (var i = minRange; i <= maxRange; i++)
			{
				foreach (var offset in TilesByDistance[i])
				{
					var t = offset + center;
					if (valid(t))
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
	}
}
