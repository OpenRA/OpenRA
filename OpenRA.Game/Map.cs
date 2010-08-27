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

namespace OpenRA
{
	public class Map
	{
		public IFolder Package;
		public string Uid;

		// Yaml map data
		[FieldLoader.Load] public bool Selectable = true;
		[FieldLoader.Load] public int MapFormat;
		[FieldLoader.Load] public string Title;
		[FieldLoader.Load] public string Description;
		[FieldLoader.Load] public string Author;
		[FieldLoader.Load] public int PlayerCount;
		[FieldLoader.Load] public string Tileset;

		public Dictionary<string, PlayerReference> Players = new Dictionary<string, PlayerReference>();
		public Dictionary<string, ActorReference> Actors = new Dictionary<string, ActorReference>();
		public List<SmudgeReference> Smudges = new List<SmudgeReference>();
		public Dictionary<string, int2> Waypoints = new Dictionary<string, int2>();

		// Rules overrides
		public List<MiniYamlNode> Rules = new List<MiniYamlNode>();

		// Binary map data
		public byte TileFormat = 1;
		[FieldLoader.Load] public int2 MapSize;

		[FieldLoader.Load] public int2 TopLeft;
		[FieldLoader.Load] public int2 BottomRight;

		public TileReference<ushort, byte>[,] MapTiles;
		public TileReference<byte, byte>[,] MapResources;
		public string [,] CustomTerrain;

		// Temporary compat hacks
		public int XOffset { get { return TopLeft.X; } }
		public int YOffset { get { return TopLeft.Y; } }
		public int Width { get { return BottomRight.X - TopLeft.X; } }
		public int Height { get { return BottomRight.Y - TopLeft.Y; } }
		public string Theater { get { return Tileset; } }
		public IEnumerable<int2> SpawnPoints { get { return Waypoints.Select(kv => kv.Value); } }
		public Rectangle Bounds { get { return Rectangle.FromLTRB(TopLeft.X, TopLeft.Y, BottomRight.X, BottomRight.Y); } }

		public Map()
		{
			MapSize = new int2(1, 1);
			MapResources = new TileReference<byte, byte>[1, 1];
			MapTiles = new TileReference<ushort, byte>[1, 1] 
				{ { new TileReference<ushort, byte> { 
					type =  (ushort)0xffffu, 
					image = (byte)0xffu, 
					index = (byte)0xffu } } };

			PlayerCount = 0;
			TopLeft = new int2(0, 0);
			BottomRight = new int2(0, 0);

			Title = "Name your map here";
			Description = "Describe your map here";
			Author = "Your name here";
		}

		class Format2ActorReference
		{
			public string Id = null;
			public string Type = null;
			public int2 Location = int2.Zero;
			public string Owner = null;
		}

		public Map(IFolder package)
		{
			Package = package;
			var yaml = new MiniYaml( null, MiniYaml.FromStream( Package.GetContent( "map.yaml" ) ) );

			// 'Simple' metadata
			FieldLoader.Load( this, yaml );

			// Waypoints
			foreach (var wp in yaml.NodesDict["Waypoints"].NodesDict)
			{
				string[] loc = wp.Value.Value.Split(',');
				Waypoints.Add(wp.Key, new int2(int.Parse(loc[0]), int.Parse(loc[1])));
			}

			// Players & Actors -- this has changed several times.
			//	- Be backwards compatible wherever possible.
			//	- Loading a map then saving it out upgrades to latest.
			// Minimum criteria for dropping a format:
			//	- There are no maps of this format left in tree

			switch (MapFormat)
			{
				case 1:
					{
						Players.Add("Neutral", new PlayerReference("Neutral", "allies", true, true));

						int actors = 0;
						foreach (var kv in yaml.NodesDict["Actors"].NodesDict)
						{
							string[] vals = kv.Value.Value.Split(' ');
							string[] loc = vals[2].Split(',');
							Actors.Add("Actor" + actors++, new ActorReference(vals[0])
							{
								new LocationInit( new int2( int.Parse( loc[ 0 ] ), int.Parse( loc[ 1 ] ) ) ),
								new OwnerInit( "Neutral" ),
							});
						}
					} break;

				case 2:
					{
						foreach (var kv in yaml.NodesDict["Players"].NodesDict)
						{
							var player = new PlayerReference(kv.Value);
							Players.Add(player.Name, player);
						}

						foreach (var kv in yaml.NodesDict["Actors"].NodesDict)
						{
							var oldActorReference = FieldLoader.Load<Format2ActorReference>(kv.Value);
							Actors.Add(oldActorReference.Id, new ActorReference(oldActorReference.Type)
							{
								new LocationInit( oldActorReference.Location ),
								new OwnerInit( oldActorReference.Owner )
							});
						}
					} break;

				case 3:
					{
						foreach (var kv in yaml.NodesDict["Players"].NodesDict)
						{
							var player = new PlayerReference(kv.Value);
							Players.Add(player.Name, player);
						}

						foreach (var kv in yaml.NodesDict["Actors"].NodesDict)
							Actors.Add(kv.Key, new ActorReference(kv.Value.Value, kv.Value.NodesDict));
					} break;

				default:
					throw new InvalidDataException("Map format {0} is not supported.".F(MapFormat));
			}

			/* hack: make some slots. */
			if (!Players.Any(p => p.Value.Playable))
			{
				for (int index = 0; index < Waypoints.Count; index++)
				{
					var p = new PlayerReference
					{
						Name = "Multi{0}".F(index),
						Race = "Random",
						Playable = true,
						DefaultStartingUnits = true
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

			CustomTerrain = new string[MapSize.X, MapSize.Y];			
			LoadUid();
			LoadBinaryData();
		}

		public void Save(string filepath)
		{
			MapFormat = 3;
			
			var root = new List<MiniYamlNode>();
			foreach (var field in new string[] {"Selectable", "MapFormat", "Title", "Description", "Author", "PlayerCount", "Tileset", "MapSize", "TopLeft", "BottomRight"})
			{
				FieldInfo f = this.GetType().GetField(field);
				if (f.GetValue(this) == null) continue;
				root.Add( new MiniYamlNode( field, FieldSaver.FormatValue( this, f ) ) );
			}

			root.Add( new MiniYamlNode( "Players", null,
				Players.Select( p => new MiniYamlNode(
					"PlayerReference@{0}".F( p.Key ),
					FieldSaver.Save( p.Value ) ) ).ToList() ) );

			root.Add( new MiniYamlNode( "Actors", null,
				Actors.Select( x => new MiniYamlNode(
					x.Key,
					x.Value.Save() ) ).ToList() ) );

			root.Add( new MiniYamlNode( "Waypoints", MiniYaml.FromDictionary<string, int2>( Waypoints ) ) );
			root.Add( new MiniYamlNode( "Smudges", MiniYaml.FromList<SmudgeReference>( Smudges ) ) );
			root.Add( new MiniYamlNode( "Rules", null, Rules ) );

			SaveBinaryData(Path.Combine(filepath, "map.bin"));
			root.WriteToFile(Path.Combine(filepath, "map.yaml"));
			SaveUid(Path.Combine(filepath, "map.uid"));
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

		public void LoadBinaryData()
		{
			using (var dataStream = Package.GetContent("map.bin"))
			{
				if (ReadByte(dataStream) != 1)
					throw new InvalidDataException("Unknown binary map format");

				// Load header info
				var width = ReadWord(dataStream);
				var height = ReadWord(dataStream);

				if (width != MapSize.X || height != MapSize.Y)
					throw new InvalidDataException("Invalid tile data");

				MapTiles = new TileReference<ushort, byte>[MapSize.X, MapSize.Y];
				MapResources = new TileReference<byte, byte>[MapSize.X, MapSize.Y];

				// Load tile data
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
					{
						ushort tile = ReadWord(dataStream);
						byte index = ReadByte(dataStream);
						byte image = (index == byte.MaxValue) ? (byte)(i % 4 + (j % 4) * 4) : index;
						MapTiles[i, j] = new TileReference<ushort, byte>(tile, index, image);
					}

				// Load resource data
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
						MapResources[i, j] = new TileReference<byte, byte>(ReadByte(dataStream), ReadByte(dataStream));
			}
		}

		public void SaveBinaryData(string filepath)
		{
			using (var dataStream = File.Create(filepath + ".tmp"))
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
						writer.Write(MapTiles[i, j].type);
						writer.Write(MapTiles[i, j].index);
					}

				// Resource data	
				for (int i = 0; i < MapSize.X; i++)
					for (int j = 0; j < MapSize.Y; j++)
					{
						writer.Write(MapResources[i, j].type);
						writer.Write(MapResources[i, j].index);
					}
			}
			File.Delete(filepath);
			File.Move(filepath + ".tmp", filepath);
		}

		public void LoadUid()
		{
			Uid = Package.GetContent("map.uid").ReadAllText();
		}

		public void SaveUid(string filename)
		{
			// UID is calculated by taking an SHA1 of the yaml and binary data
			// Read the relevant data into a buffer
			var data = Package.GetContent("map.yaml").ReadAllBytes()
				.Concat(Package.GetContent("map.bin").ReadAllBytes()).ToArray();

			// Take the SHA1
			using (var csp = SHA1.Create())
				Uid = new string(csp.ComputeHash(data).SelectMany(a => a.ToString("x2")).ToArray());

			File.WriteAllText(filename, Uid);
		}

		public bool IsInMap(int2 xy)
		{
			return IsInMap(xy.X, xy.Y);
		}

		public bool IsInMap(int x, int y)
		{
			return (x >= TopLeft.X && y >= TopLeft.Y && x < BottomRight.X && y < BottomRight.Y);
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
			MapTiles = ResizeArray(MapTiles, MapTiles[0, 0], width, height);
			MapResources = ResizeArray(MapResources, MapResources[0, 0], width, height);
			MapSize = new int2(width, height);
		}
	}
}
