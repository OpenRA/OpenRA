#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using OpenRA.FileFormats;
using System.Text;

namespace OpenRA
{
	public class Map
	{
		protected IFolder Container;
		public string Path {get; protected set;}
		
		// Yaml map data
		public string Uid { get; protected set; }
		[FieldLoader.Load] public int MapFormat;
		[FieldLoader.Load] public bool Selectable;
        [FieldLoader.Load] public bool UseAsShellmap;
		[FieldLoader.Load] public string RequiresMod;

		[FieldLoader.Load] public string Title;
		[FieldLoader.Load] public string Type = "Conquest";
		[FieldLoader.Load] public string Description;
		[FieldLoader.Load] public string Author;
		[FieldLoader.Load] public string Tileset;
		
		public Lazy<Dictionary<string, ActorReference>> Actors;

		public int PlayerCount { get { return SpawnPoints.Count(); } }
		public IEnumerable<int2> SpawnPoints { get { return Actors.Value.Values.Where(a => a.Type == "mpspawn").Select(a => a.InitDict.Get<LocationInit>().value); } }
		
		[FieldLoader.Load] public Rectangle Bounds;
				
		
		// Yaml map data
		public Dictionary<string, PlayerReference> Players = new Dictionary<string, PlayerReference>();
		public List<SmudgeReference> Smudges = new List<SmudgeReference>();

		// Rules overrides
		public List<MiniYamlNode> Rules = new List<MiniYamlNode>();

		// Sequences overrides
		public List<MiniYamlNode> Sequences = new List<MiniYamlNode>();

		// Weapon overrides
		public List<MiniYamlNode> Weapons = new List<MiniYamlNode>();

		// Voices overrides
		public List<MiniYamlNode> Voices = new List<MiniYamlNode>();

		// Binary map data
		public byte TileFormat = 1;
		[FieldLoader.Load] public int2 MapSize;

		public Lazy<TileReference<ushort, byte>[,]> MapTiles;
		public Lazy<TileReference<byte, byte>[,]> MapResources;
		public string [,] CustomTerrain;

		public Map()
		{
			// Do nothing; not a valid map (editor hack)
		}
		
		public static Map FromTileset(string tileset)
		{
			var tile = OpenRA.Rules.TileSets[tileset].Templates.First();
			Map map = new Map()
			{
				Title = "Name your map here",
				Description = "Describe your map here",
				Author = "Your name here",
				MapSize = new int2(1, 1),
				Tileset = tileset,
				MapResources = Lazy.New(() => new TileReference<byte, byte>[1, 1]),
				MapTiles = Lazy.New(() => new TileReference<ushort, byte>[1, 1]
				{ { new TileReference<ushort, byte> { 
					type = tile.Key, 
					index = (byte)0 }
				} }),
				Actors = Lazy.New(() => new Dictionary<string, ActorReference>())
			};
			
			return map;
		}

		class Format2ActorReference
		{
			public string Id = null;
			public string Type = null;
			public int2 Location = int2.Zero;
			public string Owner = null;
		}
		
		public Map(string path)
		{
			Path = path;
			Container = FileSystem.OpenPackage(path, int.MaxValue);
			var yaml = new MiniYaml( null, MiniYaml.FromStream(Container.GetContent("map.yaml")) );
			FieldLoader.Load(this, yaml);
            Uid = ComputeHash();
						
			// 'Simple' metadata
			FieldLoader.Load( this, yaml );

			// Support for formats 1-3 dropped 2011-02-11.
			// Use release-20110207 to convert older maps to format 4
			if (MapFormat < 4)
				throw new InvalidDataException("Map format {0} is not supported.\n File: {1}".F(MapFormat, path));
			
			
			Actors = Lazy.New(() =>
			{
				var ret =  new Dictionary<string, ActorReference>();
				// Load actors
				foreach (var kv in yaml.NodesDict["Actors"].NodesDict)
					ret.Add(kv.Key, new ActorReference(kv.Value.Value, kv.Value.NodesDict));
				
				// Add waypoint actors

				if (MapFormat < 5)
					foreach( var wp in yaml.NodesDict[ "Waypoints" ].NodesDict )
					{
						string[] loc = wp.Value.Value.Split( ',' );
						var a = new ActorReference("mpspawn");
						a.Add(new LocationInit(new int2( int.Parse( loc[ 0 ] ), int.Parse( loc[ 1 ] ) )));
						ret.Add(wp.Key, a);
					}
				
				return ret;
			});
			
			// Load players
			foreach (var kv in yaml.NodesDict["Players"].NodesDict)
			{
				var player = new PlayerReference(kv.Value);
				Players.Add(player.Name, player);
			}

			// Upgrade map to format 5
			if (MapFormat < 5)
			{
				// Define RequiresMod for map installer
				RequiresMod = Game.CurrentMods.Keys.First();
													
				var TopLeft = (int2)FieldLoader.GetValue( "", typeof(int2), yaml.NodesDict["TopLeft"].Value);
				var BottomRight = (int2)FieldLoader.GetValue( "", typeof(int2), yaml.NodesDict["BottomRight"].Value);
				Bounds = Rectangle.FromLTRB(TopLeft.X, TopLeft.Y, BottomRight.X, BottomRight.Y);		
				
				// Creep player
				foreach (var mp in Players.Where(p => !p.Value.NonCombatant && !p.Value.Enemies.Contains("Creeps")))
					mp.Value.Enemies = mp.Value.Enemies.Concat(new[] {"Creeps"}).ToArray();
				
				Players.Add("Creeps", new PlayerReference
				{
					Name = "Creeps",
					Race = "Random",
					NonCombatant = true,
					Enemies = Players.Keys.Where(k => k != "Neutral").ToArray()
				});
			}
			
			/* hack: make some slots. */
			if (!Players.Any(p => p.Value.Playable))
			{
				for (int index = 0; index < SpawnPoints.Count(); index++)
				{
					var p = new PlayerReference
					{
						Name = "Multi{0}".F(index),
						Race = "Random",
						Playable = true,
						DefaultStartingUnits = true,
						Enemies = new[]{"Creeps"}
					};
					Players.Add(p.Name, p);
				}
			}
						
			// Smudges
			foreach (var kv in yaml.NodesDict["Smudges"].NodesDict)
			{
				string[] vals = kv.Key.Split(' ');
				string[] loc = vals[1].Split(',');
				Smudges.Add(new SmudgeReference(vals[0], new int2(int.Parse(loc[0]), int.Parse(loc[1])), int.Parse(vals[2])));
			}

			// Rules
			Rules = yaml.NodesDict["Rules"].Nodes;

			// Sequences
			Sequences = (yaml.NodesDict.ContainsKey("Sequences")) ? yaml.NodesDict["Sequences"].Nodes : new List<MiniYamlNode>();

			// Weapons
			Weapons = (yaml.NodesDict.ContainsKey("Weapons")) ? yaml.NodesDict["Weapons"].Nodes : new List<MiniYamlNode>();
			
			// Voices
			Voices = (yaml.NodesDict.ContainsKey("Voices")) ? yaml.NodesDict["Voices"].Nodes : new List<MiniYamlNode>();

			CustomTerrain = new string[MapSize.X, MapSize.Y];
			
			MapTiles = Lazy.New(() => LoadMapTiles());
			MapResources = Lazy.New(() => LoadResourceTiles());
		}

		public void Save(string toPath)
		{			
			MapFormat = 5;
			
			var root = new List<MiniYamlNode>();
			var fields = new string[]
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
				"StartPoints"
			};
			
			foreach (var field in fields)
			{
				var f = this.GetType().GetField(field);
				if (f.GetValue(this) == null) continue;
				root.Add( new MiniYamlNode( field, FieldSaver.FormatValue( this, f ) ) );
			}

			root.Add( new MiniYamlNode( "Players", null,
				Players.Select( p => new MiniYamlNode(
					"PlayerReference@{0}".F( p.Key ),
					FieldSaver.Save( p.Value ) ) ).ToList() ) );

			root.Add( new MiniYamlNode( "Actors", null,
				Actors.Value.Select( x => new MiniYamlNode(
					x.Key,
					x.Value.Save() ) ).ToList() ) );

			root.Add(new MiniYamlNode("Smudges", MiniYaml.FromList<SmudgeReference>( Smudges )));
			root.Add(new MiniYamlNode("Rules", null, Rules));
			root.Add(new MiniYamlNode("Sequences", null, Sequences));
			root.Add(new MiniYamlNode("Weapons", null, Weapons));
			root.Add(new MiniYamlNode("Voices", null, Voices));
			
			Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>();
			entries.Add("map.bin", SaveBinaryData());
			var s = root.WriteToString();
			entries.Add("map.yaml", Encoding.UTF8.GetBytes(s));
			
			// Saving the map to a new location
			if (toPath != Path)
			{
				Path = toPath;
				
				// Create a new map package
				// TODO: Add other files (resources, rules) to the entries list
				Container = FileSystem.CreatePackage(Path, int.MaxValue, entries);
			}
			
			// Update existing package
			Container.Write(entries);
		}

		static byte ReadByte(Stream s)
		{
			int ret = s.ReadByte();
			if (ret == -1)
				throw new NotImplementedException();
			return (byte)ret;
		}

		static ushort ReadWord(Stream s)
		{
			ushort ret = ReadByte(s);
			ret |= (ushort)(ReadByte(s) << 8);

			return ret;
		}
		
		public TileReference<ushort, byte>[,] LoadMapTiles()
		{
			var tiles = new TileReference<ushort, byte>[MapSize.X, MapSize.Y];
			using (var dataStream = Container.GetContent("map.bin"))
			{
				if (ReadByte(dataStream) != 1)
					throw new InvalidDataException("Unknown binary map format");

				// Load header info
				var width = ReadWord(dataStream);
				var height = ReadWord(dataStream);

				if (width != MapSize.X || height != MapSize.Y)
					throw new InvalidDataException("Invalid tile data");


				// Load tile data
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
					{
						ushort tile = ReadWord(dataStream);
						byte index = ReadByte(dataStream);
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
				if (ReadByte(dataStream) != 1)
					throw new InvalidDataException("Unknown binary map format");

				// Load header info
				var width = ReadWord(dataStream);
				var height = ReadWord(dataStream);

				if (width != MapSize.X || height != MapSize.Y)
					throw new InvalidDataException("Invalid tile data");
				
				// Skip past tile data
				for (var i = 0; i < 3*MapSize.X*MapSize.Y; i++)
					ReadByte(dataStream);

				// Load resource data
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
				{
					byte type = ReadByte(dataStream);
					byte index = ReadByte(dataStream);
					resources[i, j] = new TileReference<byte, byte>(type, index);
				}
			}
			return resources;
		}

		public byte[] SaveBinaryData()
		{
			MemoryStream dataStream = new MemoryStream();
			using (var writer = new BinaryWriter(dataStream))
			{
				// File header consists of a version byte, followed by 2 ushorts for width and height
				writer.Write(TileFormat);
				writer.Write((ushort)MapSize.X);
				writer.Write((ushort)MapSize.Y);

				// Tile data
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
					{
						writer.Write(MapTiles.Value[i, j].type);
						var PickAny = OpenRA.Rules.TileSets[Tileset].Templates[MapTiles.Value[i, j].type].PickAny;
						writer.Write(PickAny ? (byte)(i % 4 + (j % 4) * 4) : MapTiles.Value[i, j].index);
					}

				// Resource data	
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
					{
						writer.Write(MapResources.Value[i, j].type);
						writer.Write(MapResources.Value[i, j].index);
					}
			}
			return dataStream.ToArray();
		}

		public bool IsInMap(int2 xy)
		{
			return IsInMap(xy.X, xy.Y);
		}

		public bool IsInMap(int x, int y)
		{
			return Bounds.Contains(x,y);
		}

		static T[,] ResizeArray<T>(T[,] ts, T t, int width, int height)
		{
			var result = new T[width, height];
			for (var i = 0; i < width; i++)
				for (var j = 0; j < height; j++)
					result[i, j] = i <= ts.GetUpperBound(0) && j <= ts.GetUpperBound(1)
						? ts[i, j] : t;
			return result;
		}

		public void Resize(int width, int height)		// editor magic.
		{
			MapTiles = Lazy.New(() => ResizeArray(MapTiles.Value, MapTiles.Value[0, 0], width, height));
			MapResources = Lazy.New(() => ResizeArray(MapResources.Value, MapResources.Value[0, 0], width, height));
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
	}
}
