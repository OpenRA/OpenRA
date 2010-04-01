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
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace OpenRA.FileFormats
{
	public class Map
	{
		// Yaml map data
		public int MapFormat = 1;
		public string Title;
		public string Description;
		public string Author;
		public int PlayerCount;
		public string Preview;
		public string Tileset;

		public Dictionary<string, ActorReference> Actors = new Dictionary<string, ActorReference>();
		public Dictionary<string, int2> Waypoints = new Dictionary<string, int2>();
		public Dictionary<string, MiniYaml> Rules = new Dictionary<string, MiniYaml>();
		
		// Binary map data
		public string Tiledata;
		public byte TileFormat = 1;
		public int2 MapSize;
		
		public int2 TopLeft;
		public int2 BottomRight;
		
		public TileReference<ushort,byte>[ , ] MapTiles;
		public TileReference<byte, byte>[ , ] MapResources;
		
		
		// Temporary compat hacks
		public int XOffset {get {return TopLeft.X;}}
		public int YOffset {get {return TopLeft.Y;}}
		public int Width {get {return BottomRight.X-TopLeft.X;}}
		public int Height {get {return BottomRight.Y-TopLeft.Y;}}
		public string Theater {get {return Tileset;}}
		public IEnumerable<int2> SpawnPoints {get {return Waypoints.Select(kv => kv.Value);}}
		
		List<string> SimpleFields = new List<string>() {
			"MapFormat", "Title", "Description", "Author", "PlayerCount", "Tileset", "Tiledata", "Preview", "MapSize", "TopLeft", "BottomRight"
		};
		
		public Map() {}
		
		public Map(string filename)
		{			
			var yaml = MiniYaml.FromFile(filename);
			
			// 'Simple' metadata
			foreach (var field in SimpleFields)
			{
				if (!yaml.ContainsKey(field)) continue;
				FieldLoader.LoadField(this,field,yaml[field].Value);
			}
			
			// Waypoints
			foreach (var wp in yaml["Waypoints"].Nodes)
			{
				string[] loc = wp.Value.Value.Split(',');
				Waypoints.Add(wp.Key, new int2(int.Parse(loc[0]),int.Parse(loc[1])));
			}
			
			// TODO: Players
			
			// Actors
			foreach (var kv in yaml["Actors"].Nodes.ToPairs())
			{
				string[] vals = kv.Second.Split(' ');
				string[] loc = vals[2].Split(',');
				var a = new ActorReference(vals[0], new int2(int.Parse(loc[0]),int.Parse(loc[1])) ,vals[1]);
				Actors.Add(kv.First,a);
			}
			
			// Rules
			Rules = yaml["Rules"].Nodes;
			
			LoadBinaryData(Tiledata);
		}
		
		
		public void Save(string filepath)
		{
			Dictionary<string, MiniYaml> root = new Dictionary<string, MiniYaml>();
			var d = new Dictionary<string, MiniYaml>();
			foreach (var field in SimpleFields)
			{
				FieldInfo f = this.GetType().GetField(field);
				if (f.GetValue(this) == null) continue;
				root.Add(field,new MiniYaml(FieldSaver.FormatValue(this,f),null));
			}			
			root.Add("Actors",MiniYaml.FromDictionary<string,ActorReference>(Actors));
			root.Add("Waypoints",MiniYaml.FromDictionary<string,int2>(Waypoints));

			// TODO: Players
			
			root.Add("Rules",new MiniYaml(null,Rules));
			SaveBinaryData(Tiledata);
			root.WriteToFile(filepath);
		}
		
		static byte ReadByte( Stream s )
		{
			int ret = s.ReadByte();
			if( ret == -1 )
				throw new NotImplementedException();
			return (byte)ret;
		}

		static ushort ReadWord(Stream s)
		{
			ushort ret = ReadByte(s);
			ret |= (ushort)(ReadByte(s) << 8);

			return ret;
		}
		
		public void LoadBinaryData(string filename)
		{
			Console.Write("path: {0}",filename);
			
			Stream dataStream = FileSystem.Open(filename);

			// Load header info
			byte version = ReadByte(dataStream);
			MapSize.X = ReadWord(dataStream);
			MapSize.Y = ReadWord(dataStream);
			
			MapTiles = new TileReference<ushort, byte>[ MapSize.X, MapSize.Y ];
			MapResources = new TileReference<byte, byte>[ MapSize.X, MapSize.Y ];
	
			// Load tile data
			for( int i = 0 ; i < MapSize.X ; i++ )
				for( int j = 0 ; j < MapSize.Y ; j++ )
				{
					ushort tile = ReadWord(dataStream);
					byte index = ReadByte(dataStream);
					byte image = (index == byte.MaxValue) ? (byte)( i % 4 + ( j % 4 ) * 4 ) : index;
					MapTiles[i,j] = new TileReference<ushort,byte>(tile,index, image);
				}
			
			// Load resource data
			for( int i = 0 ; i < MapSize.X ; i++ )
				for( int j = 0 ; j < MapSize.Y ; j++ )
					MapResources[i,j] = new TileReference<byte,byte>(ReadByte(dataStream),ReadByte(dataStream));
		}
		
		public void SaveBinaryData(string filepath)
		{
			FileStream dataStream = new FileStream(filepath+".tmp", FileMode.Create, FileAccess.Write);
			BinaryWriter writer = new BinaryWriter( dataStream );
			writer.BaseStream.Seek(0, SeekOrigin.Begin);
			
			// File header consists of a version byte, followed by 2 ushorts for width and height
			writer.Write(TileFormat);
			writer.Write((ushort)MapSize.X);
			writer.Write((ushort)MapSize.Y);
			
			// Tile data
			for( int i = 0 ; i < MapSize.X ; i++ )
				for( int j = 0 ; j < MapSize.Y ; j++ )
				{			
					writer.Write( MapTiles[i,j].type );
					writer.Write( MapTiles[i,j].index );
				}
						
			// Resource data	
			for( int i = 0 ; i < MapSize.X ; i++ )
				for( int j = 0 ; j < MapSize.Y ; j++ )
				{					
					writer.Write( MapResources[i,j].type );
					writer.Write( MapResources[i,j].index );
				}
			
			writer.Flush();
			writer.Close();
			File.Move(filepath+".tmp",filepath);
		}
		
		public bool IsInMap(int2 xy)
		{
			return IsInMap(xy.X,xy.Y);
		}
		
		public bool IsInMap(int x, int y)
		{
			return (x >= TopLeft.X && y >= TopLeft.Y && x < BottomRight.X && y < BottomRight.Y);
		}
		
		public void DebugContents()
		{
			foreach (var field in SimpleFields)
				Console.WriteLine("Loaded {0}: {1}", field, this.GetType().GetField(field).GetValue(this));
			
			Console.WriteLine("Loaded Waypoints:");
			foreach (var wp in Waypoints)
				Console.WriteLine("\t{0} => {1}",wp.Key,wp.Value);
			
			Console.WriteLine("Loaded Actors:");
			foreach (var wp in Actors)
				Console.WriteLine("\t{0} => {1} {2} {3}",wp.Key,wp.Value.Name, wp.Value.Owner,wp.Value.Location);
		}
	}
}
