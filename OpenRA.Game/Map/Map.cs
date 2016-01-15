#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Network;
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

	public class MapOptions
	{
		public bool? Cheats;
		public bool? Crates;
		public bool? Creeps;
		public bool? Fog;
		public bool? Shroud;
		public bool? AllyBuildRadius;
		public bool? FragileAlliances;
		public int? StartingCash;
		public string TechLevel;
		public bool ConfigurableStartingUnits = true;
		public string[] Difficulties = { };
		public bool? ShortGame;

		public void UpdateServerSettings(Session.Global settings)
		{
			if (Cheats.HasValue)
				settings.AllowCheats = Cheats.Value;
			if (Crates.HasValue)
				settings.Crates = Crates.Value;
			if (Creeps.HasValue)
				settings.Creeps = Creeps.Value;
			if (Fog.HasValue)
				settings.Fog = Fog.Value;
			if (Shroud.HasValue)
				settings.Shroud = Shroud.Value;
			if (AllyBuildRadius.HasValue)
				settings.AllyBuildRadius = AllyBuildRadius.Value;
			if (StartingCash.HasValue)
				settings.StartingCash = StartingCash.Value;
			if (FragileAlliances.HasValue)
				settings.FragileAlliances = FragileAlliances.Value;
			if (ShortGame.HasValue)
				settings.ShortGame = ShortGame.Value;
		}
	}

	public class MapVideos
	{
		public string BackgroundInfo;
		public string Briefing;
		public string GameStart;
		public string GameWon;
		public string GameLost;
	}

	[Flags]
	public enum MapVisibility
	{
		Lobby = 1,
		Shellmap = 2,
		MissionSelector = 4
	}

	public class Map
	{
		public const int MinimumSupportedMapFormat = 6;

		static readonly int[][] CellCornerHalfHeights = new int[][]
		{
			// Flat
			new[] { 0, 0, 0, 0 },

			// Slopes (two corners high)
			new[] { 0, 0, 1, 1 },
			new[] { 1, 0, 0, 1 },
			new[] { 1, 1, 0, 0 },
			new[] { 0, 1, 1, 0 },

			// Slopes (one corner high)
			new[] { 0, 0, 0, 1 },
			new[] { 1, 0, 0, 0 },
			new[] { 0, 1, 0, 0 },
			new[] { 0, 0, 1, 0 },

			// Slopes (three corners high)
			new[] { 1, 0, 1, 1 },
			new[] { 1, 1, 0, 1 },
			new[] { 1, 1, 1, 0 },
			new[] { 0, 1, 1, 1 },

			// Slopes (two corners high, one corner double high)
			new[] { 1, 0, 1, 2 },
			new[] { 2, 1, 0, 1 },
			new[] { 1, 2, 1, 0 },
			new[] { 0, 1, 2, 1 },

			// Slopes (two corners high, alternating)
			new[] { 1, 0, 1, 0 },
			new[] { 0, 1, 0, 1 },
			new[] { 1, 0, 1, 0 },
			new[] { 0, 1, 0, 1 }
		};

		public const int MaxTilesInCircleRange = 50;
		public readonly MapGrid Grid;

		[FieldLoader.Ignore] public readonly WVec[] SubCellOffsets;
		public readonly SubCell DefaultSubCell;
		public readonly SubCell LastSubCell;
		[FieldLoader.Ignore] public IReadWritePackage Container;
		public string Path { get; private set; }

		// Yaml map data
		public string Uid { get; private set; }
		public int MapFormat;
		public MapVisibility Visibility = MapVisibility.Lobby;
		public string RequiresMod;

		public string Title;
		public string Type = "Conquest";
		public string Description;
		public string Author;
		public string Tileset;
		public bool AllowStartUnitConfig = true;
		public Bitmap CustomPreview;
		public bool InvalidCustomRules { get; private set; }

		public WVec OffsetOfSubCell(SubCell subCell)
		{
			if (subCell == SubCell.Invalid || subCell == SubCell.Any)
				return WVec.Zero;

			return SubCellOffsets[(int)subCell];
		}

		[FieldLoader.LoadUsing("LoadOptions")] public MapOptions Options;

		static object LoadOptions(MiniYaml y)
		{
			var options = new MapOptions();
			var nodesDict = y.ToDictionary();
			if (nodesDict.ContainsKey("Options"))
				FieldLoader.Load(options, nodesDict["Options"]);

			return options;
		}

		[FieldLoader.LoadUsing("LoadVideos")] public MapVideos Videos;

		static object LoadVideos(MiniYaml y)
		{
			var videos = new MapVideos();
			var nodesDict = y.ToDictionary();
			if (nodesDict.ContainsKey("Videos"))
				FieldLoader.Load(videos, nodesDict["Videos"]);

			return videos;
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

		public Lazy<CPos[]> SpawnPoints;

		// Yaml map data
		[FieldLoader.Ignore] public List<MiniYamlNode> RuleDefinitions = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> SequenceDefinitions = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> VoxelSequenceDefinitions = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> WeaponDefinitions = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> VoiceDefinitions = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> MusicDefinitions = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> NotificationDefinitions = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> TranslationDefinitions = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> PlayerDefinitions = new List<MiniYamlNode>();

		[FieldLoader.Ignore] public List<MiniYamlNode> ActorDefinitions = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> SmudgeDefinitions = new List<MiniYamlNode>();

		// Binary map data
		[FieldLoader.Ignore] public byte TileFormat = 2;

		public int2 MapSize;

		[FieldLoader.Ignore] public Lazy<CellLayer<TerrainTile>> MapTiles;
		[FieldLoader.Ignore] public Lazy<CellLayer<ResourceTile>> MapResources;
		[FieldLoader.Ignore] public Lazy<CellLayer<byte>> MapHeight;

		[FieldLoader.Ignore] public CellLayer<byte> CustomTerrain;
		[FieldLoader.Ignore] CellLayer<short> cachedTerrainIndexes;

		[FieldLoader.Ignore] bool initializedCellProjection;
		[FieldLoader.Ignore] CellLayer<PPos[]> cellProjection;
		[FieldLoader.Ignore] CellLayer<List<MPos>> inverseCellProjection;

		[FieldLoader.Ignore] Lazy<TileSet> cachedTileSet;
		[FieldLoader.Ignore] Lazy<Ruleset> rules;
		public Ruleset Rules { get { return rules != null ? rules.Value : null; } }
		public SequenceProvider SequenceProvider { get { return Rules.Sequences[Tileset]; } }

		public WVec[][] CellCorners { get; private set; }
		[FieldLoader.Ignore] public ProjectedCellRegion ProjectedCellBounds;
		[FieldLoader.Ignore] public CellRegion AllCells;

		void AssertExists(string filename)
		{
			using (var s = Container.GetContent(filename))
				if (s == null)
					throw new InvalidOperationException("Required file {0} not present in this map".F(filename));
		}

		/// <summary>
		/// Initializes a new map created by the editor or importer.
		/// The map will not receive a valid UID until after it has been saved and reloaded.
		/// </summary>
		public Map(TileSet tileset, int width, int height)
		{
			var size = new Size(width, height);
			Grid = Game.ModData.Manifest.Get<MapGrid>();
			var tileRef = new TerrainTile(tileset.Templates.First().Key, (byte)0);

			Title = "Name your map here";
			Description = "Describe your map here";
			Author = "Your name here";

			MapSize = new int2(size);
			Tileset = tileset.Id;
			Videos = new MapVideos();
			Options = new MapOptions();

			MapResources = Exts.Lazy(() => new CellLayer<ResourceTile>(Grid.Type, size));

			MapTiles = Exts.Lazy(() =>
			{
				var ret = new CellLayer<TerrainTile>(Grid.Type, size);
				ret.Clear(tileRef);
				if (Grid.MaximumTerrainHeight > 0)
					ret.CellEntryChanged += UpdateProjection;
				return ret;
			});

			MapHeight = Exts.Lazy(() =>
			{
				var ret = new CellLayer<byte>(Grid.Type, size);
				ret.Clear(0);
				if (Grid.MaximumTerrainHeight > 0)
					ret.CellEntryChanged += UpdateProjection;
				return ret;
			});

			SpawnPoints = Exts.Lazy(() => new CPos[0]);

			PostInit();
		}

		/// <summary>Initializes a map loaded from disk.</summary>
		public Map(string path)
		{
			Path = path;
			Container = Game.ModData.ModFiles.OpenWritablePackage(path, int.MaxValue);

			AssertExists("map.yaml");
			AssertExists("map.bin");

			var yaml = new MiniYaml(null, MiniYaml.FromStream(Container.GetContent("map.yaml"), path));
			FieldLoader.Load(this, yaml);

			// Support for formats 1-3 dropped 2011-02-11.
			// Use release-20110207 to convert older maps to format 4
			// Use release-20110511 to convert older maps to format 5
			// Use release-20141029 to convert older maps to format 6
			if (MapFormat < MinimumSupportedMapFormat)
				throw new InvalidDataException("Map format {0} is not supported.\n File: {1}".F(MapFormat, path));

			var nd = yaml.ToDictionary();

			// Format 6 -> 7 combined the Selectable and UseAsShellmap flags into the Class enum
			if (MapFormat < 7)
			{
				MiniYaml useAsShellmap;
				if (nd.TryGetValue("UseAsShellmap", out useAsShellmap) && bool.Parse(useAsShellmap.Value))
					Visibility = MapVisibility.Shellmap;
				else if (Type == "Mission" || Type == "Campaign")
					Visibility = MapVisibility.MissionSelector;
			}

			// Format 7 -> 8 replaced normalized HSL triples with rgb(a) hex colors
			if (MapFormat < 8)
			{
				var players = yaml.Nodes.FirstOrDefault(n => n.Key == "Players");
				if (players != null)
				{
					bool noteHexColors = false;
					bool noteColorRamp = false;
					foreach (var player in players.Value.Nodes)
					{
						var colorRampNode = player.Value.Nodes.FirstOrDefault(n => n.Key == "ColorRamp");
						if (colorRampNode != null)
						{
							Color dummy;
							var parts = colorRampNode.Value.Value.Split(',');
							if (parts.Length == 3 || parts.Length == 4)
							{
								// Try to convert old normalized HSL value to a rgb hex color
								try
								{
									HSLColor color = new HSLColor(
										(byte)Exts.ParseIntegerInvariant(parts[0].Trim()).Clamp(0, 255),
										(byte)Exts.ParseIntegerInvariant(parts[1].Trim()).Clamp(0, 255),
										(byte)Exts.ParseIntegerInvariant(parts[2].Trim()).Clamp(0, 255));
									colorRampNode.Value.Value = FieldSaver.FormatValue(color);
									noteHexColors = true;
								}
								catch (Exception)
								{
									throw new InvalidDataException("Invalid ColorRamp value.\n File: " + path);
								}
							}
							else if (parts.Length != 1 || !HSLColor.TryParseRGB(parts[0], out dummy))
								throw new InvalidDataException("Invalid ColorRamp value.\n File: " + path);

							colorRampNode.Key = "Color";
							noteColorRamp = true;
						}
					}

					Console.WriteLine("Converted " + path + " to MapFormat 8.");
					if (noteHexColors)
						Console.WriteLine("ColorRamp is now called Color and uses rgb(a) hex value - rrggbb[aa].");
					else if (noteColorRamp)
						Console.WriteLine("ColorRamp is now called Color.");
				}
			}

			SpawnPoints = Exts.Lazy(() =>
			{
				var spawns = new List<CPos>();
				foreach (var kv in ActorDefinitions.Where(d => d.Value.Value == "mpspawn"))
				{
					var s = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());

					spawns.Add(s.InitDict.Get<LocationInit>().Value(null));
				}

				return spawns.ToArray();
			});

			RuleDefinitions = MiniYaml.NodesOrEmpty(yaml, "Rules");
			SequenceDefinitions = MiniYaml.NodesOrEmpty(yaml, "Sequences");
			VoxelSequenceDefinitions = MiniYaml.NodesOrEmpty(yaml, "VoxelSequences");
			WeaponDefinitions = MiniYaml.NodesOrEmpty(yaml, "Weapons");
			VoiceDefinitions = MiniYaml.NodesOrEmpty(yaml, "Voices");
			MusicDefinitions = MiniYaml.NodesOrEmpty(yaml, "Music");
			NotificationDefinitions = MiniYaml.NodesOrEmpty(yaml, "Notifications");
			TranslationDefinitions = MiniYaml.NodesOrEmpty(yaml, "Translations");
			PlayerDefinitions = MiniYaml.NodesOrEmpty(yaml, "Players");

			ActorDefinitions = MiniYaml.NodesOrEmpty(yaml, "Actors");
			SmudgeDefinitions = MiniYaml.NodesOrEmpty(yaml, "Smudges");

			MapTiles = Exts.Lazy(LoadMapTiles);
			MapResources = Exts.Lazy(LoadResourceTiles);
			MapHeight = Exts.Lazy(LoadMapHeight);

			Grid = Game.ModData.Manifest.Get<MapGrid>();

			SubCellOffsets = Grid.SubCellOffsets;
			LastSubCell = (SubCell)(SubCellOffsets.Length - 1);
			DefaultSubCell = (SubCell)Grid.SubCellDefaultIndex;

			if (Container.Exists("map.png"))
				using (var dataStream = Container.GetContent("map.png"))
					CustomPreview = new Bitmap(dataStream);

			PostInit();

			// The Uid is calculated from the data on-disk, so
			// format changes must be flushed to disk.
			// TODO: this isn't very nice
			if (MapFormat < 8)
				Save(path);

			Uid = ComputeHash();
		}

		void PostInit()
		{
			rules = Exts.Lazy(() =>
			{
				try
				{
					return Game.ModData.RulesetCache.Load(this);
				}
				catch (Exception e)
				{
					InvalidCustomRules = true;
					Log.Write("debug", "Failed to load rules for {0} with error {1}", Title, e.Message);
				}

				return Game.ModData.DefaultRules;
			});

			cachedTileSet = Exts.Lazy(() => Rules.TileSets[Tileset]);

			var tl = new MPos(0, 0).ToCPos(this);
			var br = new MPos(MapSize.X - 1, MapSize.Y - 1).ToCPos(this);
			AllCells = new CellRegion(Grid.Type, tl, br);

			var btl = new PPos(Bounds.Left, Bounds.Top);
			var bbr = new PPos(Bounds.Right - 1, Bounds.Bottom - 1);
			SetBounds(btl, bbr);

			CustomTerrain = new CellLayer<byte>(this);
			foreach (var uv in AllCells.MapCoords)
				CustomTerrain[uv] = byte.MaxValue;

			var leftDelta = Grid.Type == MapGridType.RectangularIsometric ? new WVec(-512, 0, 0) : new WVec(-512, -512, 0);
			var topDelta = Grid.Type == MapGridType.RectangularIsometric ? new WVec(0, -512, 0) : new WVec(512, -512, 0);
			var rightDelta = Grid.Type == MapGridType.RectangularIsometric ? new WVec(512, 0, 0) : new WVec(512, 512, 0);
			var bottomDelta = Grid.Type == MapGridType.RectangularIsometric ? new WVec(0, 512, 0) : new WVec(-512, 512, 0);
			CellCorners = CellCornerHalfHeights.Select(ramp => new WVec[]
			{
				leftDelta + new WVec(0, 0, 512 * ramp[0]),
				topDelta + new WVec(0, 0, 512 * ramp[1]),
				rightDelta + new WVec(0, 0, 512 * ramp[2]),
				bottomDelta + new WVec(0, 0, 512 * ramp[3])
			}).ToArray();
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
			var mapHeight = MapHeight.Value;
			if (!mapHeight.Contains(uv))
				return NoProjectedCells;

			var height = mapHeight[uv];
			if (height == 0)
				return new[] { (PPos)uv };

			// Odd-height ramps get bumped up a level to the next even height layer
			if ((height & 1) == 1)
			{
				var ti = cachedTileSet.Value.GetTileInfo(MapTiles.Value[uv]);
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

		public void Save(string toPath)
		{
			MapFormat = 8;

			var root = new List<MiniYamlNode>();
			var fields = new[]
			{
				"MapFormat",
				"RequiresMod",
				"Title",
				"Description",
				"Author",
				"Tileset",
				"MapSize",
				"Bounds",
				"Visibility",
				"Type",
			};

			foreach (var field in fields)
			{
				var f = this.GetType().GetField(field);
				if (f.GetValue(this) == null)
					continue;
				root.Add(new MiniYamlNode(field, FieldSaver.FormatValue(this, f)));
			}

			root.Add(new MiniYamlNode("Videos", FieldSaver.SaveDifferences(Videos, new MapVideos())));

			root.Add(new MiniYamlNode("Options", FieldSaver.SaveDifferences(Options, new MapOptions())));

			root.Add(new MiniYamlNode("Players", null, PlayerDefinitions));

			root.Add(new MiniYamlNode("Actors", null, ActorDefinitions));
			root.Add(new MiniYamlNode("Smudges", null, SmudgeDefinitions));
			root.Add(new MiniYamlNode("Rules", null, RuleDefinitions));
			root.Add(new MiniYamlNode("Sequences", null, SequenceDefinitions));
			root.Add(new MiniYamlNode("VoxelSequences", null, VoxelSequenceDefinitions));
			root.Add(new MiniYamlNode("Weapons", null, WeaponDefinitions));
			root.Add(new MiniYamlNode("Voices", null, VoiceDefinitions));
			root.Add(new MiniYamlNode("Music", null, MusicDefinitions));
			root.Add(new MiniYamlNode("Notifications", null, NotificationDefinitions));
			root.Add(new MiniYamlNode("Translations", null, TranslationDefinitions));

			var entries = new Dictionary<string, byte[]>();
			entries.Add("map.bin", SaveBinaryData());
			var s = root.WriteToString();
			entries.Add("map.yaml", Encoding.UTF8.GetBytes(s));

			// Add any custom assets
			if (Container != null)
			{
				foreach (var file in Container.AllFileNames())
				{
					if (file == "map.bin" || file == "map.yaml")
						continue;

					entries.Add(file, Container.GetContent(file).ReadAllBytes());
				}
			}

			// Saving the map to a new location
			if (toPath != Path || Container == null)
			{
				Path = toPath;

				// Create a new map package
				Container = Game.ModData.ModFiles.CreatePackage(Path, int.MaxValue, entries);
			}

			// Update existing package
			Container.Write(entries);

			// Update UID to match the newly saved data
			Uid = ComputeHash();
		}

		public CellLayer<TerrainTile> LoadMapTiles()
		{
			var tiles = new CellLayer<TerrainTile>(this);
			using (var s = Container.GetContent("map.bin"))
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

							tiles[new MPos(i, j)] = new TerrainTile(tile, index);
						}
					}
				}
			}

			if (Grid.MaximumTerrainHeight > 0)
				tiles.CellEntryChanged += UpdateProjection;

			return tiles;
		}

		public CellLayer<byte> LoadMapHeight()
		{
			var tiles = new CellLayer<byte>(this);
			using (var s = Container.GetContent("map.bin"))
			{
				var header = new BinaryDataHeader(s, MapSize);
				if (header.HeightsOffset > 0)
				{
					s.Position = header.HeightsOffset;
					for (var i = 0; i < MapSize.X; i++)
						for (var j = 0; j < MapSize.Y; j++)
							tiles[new MPos(i, j)] = s.ReadUInt8().Clamp((byte)0, Grid.MaximumTerrainHeight);
				}
			}

			if (Grid.MaximumTerrainHeight > 0)
				tiles.CellEntryChanged += UpdateProjection;

			return tiles;
		}

		public CellLayer<ResourceTile> LoadResourceTiles()
		{
			var resources = new CellLayer<ResourceTile>(this);

			using (var s = Container.GetContent("map.bin"))
			{
				var header = new BinaryDataHeader(s, MapSize);
				if (header.ResourcesOffset > 0)
				{
					s.Position = header.ResourcesOffset;
					for (var i = 0; i < MapSize.X; i++)
					{
						for (var j = 0; j < MapSize.Y; j++)
						{
							var type = s.ReadUInt8();
							var density = s.ReadUInt8();
							resources[new MPos(i, j)] = new ResourceTile(type, density);
						}
					}
				}
			}

			return resources;
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
							var tile = MapTiles.Value[new MPos(i, j)];
							writer.Write(tile.Type);
							writer.Write(tile.Index);
						}
					}
				}

				// Height data
				if (heightsOffset != 0)
					for (var i = 0; i < MapSize.X; i++)
						for (var j = 0; j < MapSize.Y; j++)
							writer.Write(MapHeight.Value[new MPos(i, j)]);

				// Resource data
				if (resourcesOffset != 0)
				{
					for (var i = 0; i < MapSize.X; i++)
					{
						for (var j = 0; j < MapSize.Y; j++)
						{
							var tile = MapResources.Value[new MPos(i, j)];
							writer.Write(tile.Type);
							writer.Write(tile.Index);
						}
					}
				}
			}

			return dataStream.ToArray();
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

			foreach (var puv in ProjectedCellsCovering(uv))
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
			var z = MapHeight.Value.Contains(cell) ? 512 * MapHeight.Value[cell] : 0;
			return new WPos(512 * (cell.X - cell.Y + 1), 512 * (cell.X + cell.Y + 1), z);
		}

		public WPos CenterOfSubCell(CPos cell, SubCell subCell)
		{
			var index = (int)subCell;
			if (index >= 0 && index <= SubCellOffsets.Length)
				return CenterOfCell(cell) + SubCellOffsets[index];
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
			return Traits.Util.GetFacing(CenterOfCell(towards) - CenterOfCell(cell), fallbackfacing);
		}

		public void Resize(int width, int height)		// editor magic.
		{
			var oldMapTiles = MapTiles.Value;
			var oldMapResources = MapResources.Value;
			var oldMapHeight = MapHeight.Value;
			var newSize = new Size(width, height);

			MapTiles = Exts.Lazy(() => CellLayer.Resize(oldMapTiles, newSize, oldMapTiles[MPos.Zero]));
			MapResources = Exts.Lazy(() => CellLayer.Resize(oldMapResources, newSize, oldMapResources[MPos.Zero]));
			MapHeight = Exts.Lazy(() => CellLayer.Resize(oldMapHeight, newSize, oldMapHeight[MPos.Zero]));
			MapSize = new int2(newSize);

			var tl = new MPos(0, 0).ToCPos(this);
			var br = new MPos(MapSize.X - 1, MapSize.Y - 1).ToCPos(this);
			AllCells = new CellRegion(Grid.Type, tl, br);
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

		string ComputeHash()
		{
			// UID is calculated by taking an SHA1 of the yaml and binary data
			using (var ms = new MemoryStream())
			{
				// Read the relevant data into the buffer
				using (var s = Container.GetContent("map.yaml"))
					s.CopyTo(ms);
				using (var s = Container.GetContent("map.bin"))
					s.CopyTo(ms);

				// Take the SHA1
				ms.Seek(0, SeekOrigin.Begin);
				using (var csp = SHA1.Create())
					return new string(csp.ComputeHash(ms).SelectMany(a => a.ToString("x2")).ToArray());
			}
		}

		public void FixOpenAreas(Ruleset rules)
		{
			var r = new Random();
			var tileset = rules.TileSets[Tileset];

			for (var j = Bounds.Top; j < Bounds.Bottom; j++)
			{
				for (var i = Bounds.Left; i < Bounds.Right; i++)
				{
					var type = MapTiles.Value[new MPos(i, j)].Type;
					var index = MapTiles.Value[new MPos(i, j)].Index;
					if (!tileset.Templates.ContainsKey(type))
					{
						Console.WriteLine("Unknown Tile ID {0}".F(type));
						continue;
					}

					var template = tileset.Templates[type];
					if (!template.PickAny)
						continue;

					index = (byte)r.Next(0, template.TilesCount);
					MapTiles.Value[new MPos(i, j)] = new TerrainTile(type, index);
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
				MapTiles.Value.CellEntryChanged += invalidateTerrainIndex;
			}

			var uv = cell.ToMPos(this);
			var terrainIndex = cachedTerrainIndexes[uv];

			// PERF: Cache terrain indexes per cell on demand.
			if (terrainIndex == InvalidCachedTerrainIndex)
			{
				var custom = CustomTerrain[uv];
				terrainIndex = cachedTerrainIndexes[uv] =
					custom != byte.MaxValue ? custom : cachedTileSet.Value.GetTerrainIndex(MapTiles.Value[uv]);
			}

			return (byte)terrainIndex;
		}

		public TerrainTypeInfo GetTerrainInfo(CPos cell)
		{
			return cachedTileSet.Value[GetTerrainIndex(cell)];
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

		public CPos ChooseRandomEdgeCell(MersenneTwister rand)
		{
			List<MPos> cells;
			do
			{
				var isU = rand.Next(2) == 0;
				var edge = rand.Next(2) == 0;
				var u = isU ? rand.Next(Bounds.Left, Bounds.Right) : (edge ? Bounds.Left : Bounds.Right);
				var v = !isU ? rand.Next(Bounds.Top, Bounds.Bottom) : (edge ? Bounds.Top : Bounds.Bottom);

				cells = Unproject(new PPos(u, v));
			} while (!cells.Any());

			return cells.Random(rand).ToCPos(Grid.Type);
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

			if (maxRange > TilesByDistance.Length)
				throw new ArgumentOutOfRangeException("maxRange", "The requested range ({0}) exceeds the maximum allowed ({1})".F(maxRange, MaxTilesInCircleRange));

			Func<CPos, bool> valid = Contains;
			if (allowOutsideBounds)
				valid = MapTiles.Value.Contains;

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
	}
}
