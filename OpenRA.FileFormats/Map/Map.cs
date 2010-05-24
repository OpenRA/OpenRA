#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace OpenRA.FileFormats
{
	public class Map
	{
		public IFolder Package;
		public string Uid;

		// Yaml map data
		public bool Selectable = true;
		public int MapFormat = 1;
		public string Title;
		public string Description;
		public string Author;
		public int PlayerCount;
		public string Tileset;

		public Dictionary<string, PlayerReference> Players = new Dictionary<string, PlayerReference>();
		public Dictionary<string, ActorReference> Actors = new Dictionary<string, ActorReference>();
		public List<SmudgeReference> Smudges = new List<SmudgeReference>();
		public Dictionary<string, int2> Waypoints = new Dictionary<string, int2>();

		// Rules overrides
		public Dictionary<string, MiniYaml> Rules = new Dictionary<string, MiniYaml>();
		public Dictionary<string, MiniYaml> Weapons = new Dictionary<string, MiniYaml>();
		public Dictionary<string, MiniYaml> Voices = new Dictionary<string, MiniYaml>();
		public Dictionary<string, MiniYaml> Music = new Dictionary<string, MiniYaml>();
		public Dictionary<string, MiniYaml> Terrain = new Dictionary<string, MiniYaml>();
		
		// Binary map data
		public byte TileFormat = 1;
		public int2 MapSize;

		public int2 TopLeft;
		public int2 BottomRight;

		public Rectangle Bounds { get { return Rectangle.FromLTRB(TopLeft.X, TopLeft.Y, BottomRight.X, BottomRight.Y); } }

		public TileReference<ushort, byte>[,] MapTiles;
		public TileReference<byte, byte>[,] MapResources;


		// Temporary compat hacks
		public int XOffset { get { return TopLeft.X; } }
		public int YOffset { get { return TopLeft.Y; } }
		public int Width { get { return BottomRight.X - TopLeft.X; } }
		public int Height { get { return BottomRight.Y - TopLeft.Y; } }
		public string Theater { get { return Tileset; } }
		public IEnumerable<int2> SpawnPoints { get { return Waypoints.Select(kv => kv.Value); } }

		static List<string> SimpleFields = new List<string>() {
			"Selectable", "MapFormat", "Title", "Description", "Author", "PlayerCount", "Tileset", "MapSize", "TopLeft", "BottomRight"
		};

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
			TopLeft = new int2(0,0);
			BottomRight = new int2(0,0);

			Tileset = "TEMPERAT";
			Players.Add("Neutral", new PlayerReference("Neutral", "neutral", "allies", true, true));

			Title = "Name your map here";
			Description = "Describe your map here";
			Author = "Your name here";
		}

		public Map(IFolder package)
		{
			Package = package;
			var yaml = MiniYaml.FromStream(Package.GetContent("map.yaml"));

			// 'Simple' metadata
			FieldLoader.LoadFields(this, yaml, SimpleFields);

			// Waypoints
			foreach (var wp in yaml["Waypoints"].Nodes)
			{
				string[] loc = wp.Value.Value.Split(',');
				Waypoints.Add(wp.Key, new int2(int.Parse(loc[0]), int.Parse(loc[1])));
			}
			
			// Players
			if (MapFormat < 2)
			{
				Players.Add("Neutral", new PlayerReference("Neutral", "neutral", "allies", true, true));
			}
			else
			{
				foreach (var kv in yaml["Players"].Nodes)
				{
					var player = new PlayerReference(kv.Value);
					Players.Add(player.Name, player);
				}
			}
			
			// Actors
			if (MapFormat == 1 )
			{
				int actors = 0;
				foreach (var kv in yaml["Actors"].Nodes)
				{
					string[] vals = kv.Value.Value.Split(' ');
					string[] loc = vals[2].Split(',');
					var a = new ActorReference("Actor"+actors++, vals[0], new int2(int.Parse(loc[0]), int.Parse(loc[1])), "Neutral");
					Actors.Add(a.Id, a);
				}
			}
			else if (MapFormat == 2)
			{
				int actors = 0;
				foreach (var kv in yaml["Actors"].Nodes)
				{
					string[] vals = kv.Value.Value.Split(' ');
					string[] loc = vals[2].Split(',');
					var a = new ActorReference("Actor"+actors++, vals[0], new int2(int.Parse(loc[0]), int.Parse(loc[1])), vals[1]);
					Actors.Add(kv.Key, a);
				}
			}
			else
			{
				foreach (var kv in yaml["Actors"].Nodes)
				{
					var player = new ActorReference(kv.Value);
					Actors.Add(player.Id, player);
				}
			}

			// Smudges
			foreach (var kv in yaml["Smudges"].Nodes)
			{
				string[] vals = kv.Key.Split(' ');
				string[] loc = vals[1].Split(',');
				Smudges.Add(new SmudgeReference(vals[0], new int2(int.Parse(loc[0]), int.Parse(loc[1])), int.Parse(vals[2])));
			}

			// Rules
			Rules = yaml["Rules"].Nodes;

			LoadUid();
			LoadBinaryData();
		}

		public void Save(string filepath)
		{
			MapFormat = 3;
			
			Dictionary<string, MiniYaml> root = new Dictionary<string, MiniYaml>();
			foreach (var field in SimpleFields)
			{
				FieldInfo f = this.GetType().GetField(field);
				if (f.GetValue(this) == null) continue;
				root.Add(field, new MiniYaml(FieldSaver.FormatValue(this, f), null));
			}
			
			Dictionary<string, MiniYaml> playerYaml = new Dictionary<string, MiniYaml>();
			foreach(var p in Players)
				playerYaml.Add("PlayerReference@{0}".F(p.Key), FieldSaver.Save(p.Value));
			root.Add("Players",new MiniYaml(null, playerYaml));
			
			Dictionary<string, MiniYaml> actorYaml = new Dictionary<string, MiniYaml>();
			foreach(var p in Actors)
				actorYaml.Add("ActorReference@{0}".F(p.Key), FieldSaver.Save(p.Value));
			root.Add("Actors",new MiniYaml(null, actorYaml));
			
			root.Add("Waypoints", MiniYaml.FromDictionary<string, int2>(Waypoints));
			root.Add("Smudges", MiniYaml.FromList<SmudgeReference>(Smudges));
			root.Add("Rules", new MiniYaml(null, Rules));
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
			var data = Exts.ReadAllBytes(Package.GetContent("map.yaml"))
				.Concat(Exts.ReadAllBytes(Package.GetContent("map.bin"))).ToArray();

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
