#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA
{
	public class MapOptions
	{
		public bool? Cheats;
		public bool? Crates;
		public bool? Fog;
		public bool? Shroud;
		public bool? AllyBuildRadius;
		public bool? FragileAlliances;
		public int? StartingCash;
		public bool ConfigurableStartingUnits = true;
		public string[] Difficulties = { };

		public void UpdateServerSettings(Session.Global settings)
		{
			if (Cheats.HasValue)
				settings.AllowCheats = Cheats.Value;
			if (Crates.HasValue)
				settings.Crates = Crates.Value;
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
		}
	}

	public class Map
	{
		[FieldLoader.Ignore] public IFolder Container;
		public string Path { get; private set; }

		// Yaml map data
		public string Uid { get; private set; }
		public int MapFormat;
		public bool Selectable = true;
		public bool UseAsShellmap;
		public string RequiresMod;

		public string Title;
		public string Type = "Conquest";
		public string Description;
		public string Author;
		public string Tileset;
		public bool AllowStartUnitConfig = true;

		[FieldLoader.LoadUsing("LoadOptions")]
		public MapOptions Options;

		static object LoadOptions(MiniYaml y)
		{
			var options = new MapOptions();
			if (y.NodesDict.ContainsKey("Options"))
				FieldLoader.Load(options, y.NodesDict["Options"]);

			return options;
		}

		[FieldLoader.Ignore] public Lazy<Dictionary<string, ActorReference>> Actors;

		public int PlayerCount { get { return Players.Count(p => p.Value.Playable); } }

		public Rectangle Bounds;

		// Yaml map data
		[FieldLoader.Ignore] public Dictionary<string, PlayerReference> Players = new Dictionary<string, PlayerReference>();
		[FieldLoader.Ignore] public Lazy<List<SmudgeReference>> Smudges;

		[FieldLoader.Ignore] public List<MiniYamlNode> Rules = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> Sequences = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> VoxelSequences = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> Weapons = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> Voices = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> Notifications = new List<MiniYamlNode>();
		[FieldLoader.Ignore] public List<MiniYamlNode> Translations = new List<MiniYamlNode>();

		// Binary map data
		[FieldLoader.Ignore] public byte TileFormat = 1;
		public int2 MapSize;

		[FieldLoader.Ignore] public Lazy<TileReference<ushort, byte>[,]> MapTiles;
		[FieldLoader.Ignore] public Lazy<TileReference<byte, byte>[,]> MapResources;
		[FieldLoader.Ignore] public string[,] CustomTerrain;

		public static Map FromTileset(TileSet tileset)
		{
			var tile = tileset.Templates.First();
			var tileRef = new TileReference<ushort, byte> { Type = tile.Key, Index = (byte)0 };

			Map map = new Map()
			{
				Title = "Name your map here",
				Description = "Describe your map here",
				Author = "Your name here",
				MapSize = new int2(1, 1),
				Tileset = tileset.Id,
				Options = new MapOptions(),
				MapResources = Lazy.New(() => new TileReference<byte, byte>[1, 1]),
				MapTiles = Lazy.New(() => new TileReference<ushort, byte>[1, 1] { { tileRef } }),
				Actors = Lazy.New(() => new Dictionary<string, ActorReference>()),
				Smudges = Lazy.New(() => new List<SmudgeReference>())
			};

			return map;
		}

		void AssertExists(string filename)
		{
			using (var s = Container.GetContent(filename))
				if (s == null)
					throw new InvalidOperationException("Required file {0} not present in this map".F(filename));
		}

		// Stub constructor that doesn't produce a valid map, but is
		// sufficient for loading a mod to the content-install panel
		public Map() { }

		// The standard constructor for most purposes
		public Map(string path) : this(path, null) { }

		// Support upgrading format 5 maps to a more
		// recent version by defining upgradeForMod.
		public Map(string path, string upgradeForMod)
		{
			Path = path;
			Container = FileSystem.OpenPackage(path, null, int.MaxValue);

			AssertExists("map.yaml");
			AssertExists("map.bin");

			var yaml = new MiniYaml(null, MiniYaml.FromStream(Container.GetContent("map.yaml")));
			FieldLoader.Load(this, yaml);

			// Support for formats 1-3 dropped 2011-02-11.
			// Use release-20110207 to convert older maps to format 4
			// Use release-20110511 to convert older maps to format 5
			if (MapFormat < 5)
				throw new InvalidDataException("Map format {0} is not supported.\n File: {1}".F(MapFormat, path));

			// Format 5 -> 6 enforces the use of RequiresMod
			if (MapFormat == 5)
			{
				if (upgradeForMod == null)
					throw new InvalidDataException("Map format {0} is not supported, but can be upgraded.\n File: {1}".F(MapFormat, path));

				Console.WriteLine("Upgrading {0} from Format 5 to Format 6", path);

				// TODO: This isn't very nice, but there is no other consistent way
				// of finding the mod early during the engine initialization.
				RequiresMod = upgradeForMod;
			}

			// Load players
			foreach (var kv in yaml.NodesDict["Players"].NodesDict)
			{
				var player = new PlayerReference(kv.Value);
				Players.Add(player.Name, player);
			}

			Actors = Lazy.New(() =>
			{
				var ret = new Dictionary<string, ActorReference>();
				foreach (var kv in yaml.NodesDict["Actors"].NodesDict)
					ret.Add(kv.Key, new ActorReference(kv.Value.Value, kv.Value.NodesDict));
				return ret;
			});

			// Smudges
			Smudges = Lazy.New(() =>
			{
				var ret = new List<SmudgeReference>();
				foreach (var kv in yaml.NodesDict["Smudges"].NodesDict)
				{
					var vals = kv.Key.Split(' ');
					var loc = vals[1].Split(',');
					ret.Add(new SmudgeReference(vals[0], new int2(int.Parse(loc[0]), int.Parse(loc[1])), int.Parse(vals[2])));
				}

				return ret;
			});

			Rules = MiniYaml.NodesOrEmpty(yaml, "Rules");
			Sequences = MiniYaml.NodesOrEmpty(yaml, "Sequences");
			VoxelSequences = MiniYaml.NodesOrEmpty(yaml, "VoxelSequences");
			Weapons = MiniYaml.NodesOrEmpty(yaml, "Weapons");
			Voices = MiniYaml.NodesOrEmpty(yaml, "Voices");
			Notifications = MiniYaml.NodesOrEmpty(yaml, "Notifications");
			Translations = MiniYaml.NodesOrEmpty(yaml, "Translations");

			CustomTerrain = new string[MapSize.X, MapSize.Y];

			MapTiles = Lazy.New(() => LoadMapTiles());
			MapResources = Lazy.New(() => LoadResourceTiles());

			// The Uid is calculated from the data on-disk, so
			// format changes must be flushed to disk.
			// TODO: this isn't very nice
			if (MapFormat < 6)
				Save(path);

			Uid = ComputeHash();
		}

		public CPos[] GetSpawnPoints()
		{
			return Actors.Value.Values
				.Where(a => a.Type == "mpspawn")
				.Select(a => (CPos)a.InitDict.Get<LocationInit>().value)
				.ToArray();
		}

		public void Save(string toPath)
		{
			MapFormat = 6;

			var root = new List<MiniYamlNode>();
			var fields = new[]
			{
				"Selectable",
				"MapFormat",
				"RequiresMod",
				"Title",
				"Description",
				"Author",
				"Tileset",
				"MapSize",
				"Bounds",
				"UseAsShellmap",
				"Type",
			};

			foreach (var field in fields)
			{
				var f = this.GetType().GetField(field);
				if (f.GetValue(this) == null)
					continue;
				root.Add(new MiniYamlNode(field, FieldSaver.FormatValue(this, f)));
			}

			root.Add(new MiniYamlNode("Options", FieldSaver.SaveDifferences(Options, new MapOptions())));

			root.Add(new MiniYamlNode("Players", null,
				Players.Select(p => new MiniYamlNode("PlayerReference@{0}".F(p.Key), FieldSaver.SaveDifferences(p.Value, new PlayerReference()))).ToList())
			);

			root.Add(new MiniYamlNode("Actors", null,
				Actors.Value.Select(x => new MiniYamlNode(x.Key, x.Value.Save())).ToList())
			);

			root.Add(new MiniYamlNode("Smudges", MiniYaml.FromList<SmudgeReference>(Smudges.Value)));
			root.Add(new MiniYamlNode("Rules", null, Rules));
			root.Add(new MiniYamlNode("Sequences", null, Sequences));
			root.Add(new MiniYamlNode("VoxelSequences", null, VoxelSequences));
			root.Add(new MiniYamlNode("Weapons", null, Weapons));
			root.Add(new MiniYamlNode("Voices", null, Voices));
			root.Add(new MiniYamlNode("Notifications", null, Notifications));
			root.Add(new MiniYamlNode("Translations", null, Translations));

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
			if (toPath != Path)
			{
				Path = toPath;

				// Create a new map package
				Container = FileSystem.CreatePackage(Path, int.MaxValue, entries);
			}

			// Update existing package
			Container.Write(entries);
		}

		public TileReference<ushort, byte>[,] LoadMapTiles()
		{
			var tiles = new TileReference<ushort, byte>[MapSize.X, MapSize.Y];
			using (var dataStream = Container.GetContent("map.bin"))
			{
				if (dataStream.ReadUInt8() != 1)
					throw new InvalidDataException("Unknown binary map format");

				// Load header info
				var width = dataStream.ReadUInt16();
				var height = dataStream.ReadUInt16();

				if (width != MapSize.X || height != MapSize.Y)
					throw new InvalidDataException("Invalid tile data");

				// Load tile data
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
					{
						var tile = dataStream.ReadUInt16();
						var index = dataStream.ReadUInt8();
						if (index == byte.MaxValue)
							index = (byte)(i % 4 + (j % 4) * 4);

						tiles[i, j] = new TileReference<ushort, byte>(tile, index);
					}
			}

			return tiles;
		}

		public TileReference<byte, byte>[,] LoadResourceTiles()
		{
			var resources = new TileReference<byte, byte>[MapSize.X, MapSize.Y];

			using (var dataStream = Container.GetContent("map.bin"))
			{
				if (dataStream.ReadUInt8() != 1)
					throw new InvalidDataException("Unknown binary map format");

				// Load header info
				var width = dataStream.ReadUInt16();
				var height = dataStream.ReadUInt16();

				if (width != MapSize.X || height != MapSize.Y)
					throw new InvalidDataException("Invalid tile data");

				// Skip past tile data
				dataStream.Seek(3 * MapSize.X * MapSize.Y, SeekOrigin.Current);

				// Load resource data
				for (var i = 0; i < MapSize.X; i++)
					for (var j = 0; j < MapSize.Y; j++)
				{
					var type = dataStream.ReadUInt8();
					var index = dataStream.ReadUInt8();
					resources[i, j] = new TileReference<byte, byte>(type, index);
				}
			}

			return resources;
		}

		public byte[] SaveBinaryData()
		{
			var dataStream = new MemoryStream();
			using (var writer = new BinaryWriter(dataStream))
			{
				// File header consists of a version byte, followed by 2 ushorts for width and height
				writer.Write(TileFormat);
				writer.Write((ushort)MapSize.X);
				writer.Write((ushort)MapSize.Y);

				// Tile data
				for (var i = 0; i < MapSize.X; i++)
					for (var j = 0; j < MapSize.Y; j++)
					{
						writer.Write(MapTiles.Value[i, j].Type);
						writer.Write(MapTiles.Value[i, j].Index);
					}

				// Resource data
				for (var i = 0; i < MapSize.X; i++)
					for (var j = 0; j < MapSize.Y; j++)
					{
						writer.Write(MapResources.Value[i, j].Type);
						writer.Write(MapResources.Value[i, j].Index);
					}
			}

			return dataStream.ToArray();
		}

		public bool IsInMap(CPos xy) { return IsInMap(xy.X, xy.Y); }
		public bool IsInMap(int x, int y) { return Bounds.Contains(x, y); }

		public void Resize(int width, int height)		// editor magic.
		{
			var oldMapTiles = MapTiles.Value;
			var oldMapResources = MapResources.Value;

			MapTiles = Lazy.New(() => Exts.ResizeArray(oldMapTiles, oldMapTiles[0, 0], width, height));
			MapResources = Lazy.New(() => Exts.ResizeArray(oldMapResources, oldMapResources[0, 0], width, height));
			MapSize = new int2(width, height);
		}

		public void ResizeCordon(int left, int top, int right, int bottom)
		{
			Bounds = Rectangle.FromLTRB(left, top, right, bottom);
		}

		string ComputeHash()
		{
			// UID is calculated by taking an SHA1 of the yaml and binary data
			// Read the relevant data into a buffer
			var data = Container.GetContent("map.yaml").ReadAllBytes()
				.Concat(Container.GetContent("map.bin").ReadAllBytes()).ToArray();

			// Take the SHA1
			using (var csp = SHA1.Create())
				return new string(csp.ComputeHash(data).SelectMany(a => a.ToString("x2")).ToArray());
		}

		public void MakeDefaultPlayers()
		{
			var firstRace = OpenRA.Rules.Info["world"].Traits
				.WithInterface<CountryInfo>().First(c => c.Selectable).Race;

			if (!Players.ContainsKey("Neutral"))
				Players.Add("Neutral", new PlayerReference
				{
					Name = "Neutral",
					Race = firstRace,
					OwnsWorld = true,
					NonCombatant = true
				});

			var numSpawns = GetSpawnPoints().Length;
			for (var index = 0; index < numSpawns; index++)
			{
				if (Players.ContainsKey("Multi{0}".F(index)))
					continue;

				var p = new PlayerReference
				{
					Name = "Multi{0}".F(index),
					Race = "Random",
					Playable = true,
					Enemies = new[] { "Creeps" }
				};
				Players.Add(p.Name, p);
			}

			Players.Add("Creeps", new PlayerReference
			{
				Name = "Creeps",
				Race = firstRace,
				NonCombatant = true,
				Enemies = Players.Where(p => p.Value.Playable).Select(p => p.Key).ToArray()
			});
		}

		public void FixOpenAreas()
		{
			var r = new Random();
			var tileset = OpenRA.Rules.TileSets[Tileset];

			for (var j = Bounds.Top; j < Bounds.Bottom; j++)
			{
				for (var i = Bounds.Left; i < Bounds.Right; i++)
				{
					var tr = MapTiles.Value[i, j];
					if (!tileset.Templates.ContainsKey(tr.Type))
					{
						Console.WriteLine("Unknown Tile ID {0}".F(tr.Type));
						continue;
					}
					var template = tileset.Templates[tr.Type];
					if (!template.PickAny)
						continue;
					tr.Index = (byte)r.Next(0, template.Tiles.Count);
					MapTiles.Value[i, j] = tr;
				}
			}
		}
	}
}
