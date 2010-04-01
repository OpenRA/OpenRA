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
using OpenRA;
using OpenRA.FileFormats;

namespace MapConverter
{
	public class MapConverter
	{
		public readonly int INIFormat;

		public readonly int MapSize;
		public readonly int XOffset;
		public readonly int YOffset;

		public readonly int Width;
		public readonly int Height;
		public NewMap Map = new NewMap();

		static string Truncate( string s, int maxLength )
		{
			return s.Length <= maxLength ? s : s.Substring(0,maxLength );
		}
		
		
		static string[] raOverlayNames =
		{
			"sbag", "cycl", "brik", "fenc", "wood",
			"gold01", "gold02", "gold03", "gold04",
			"gem01", "gem02", "gem03", "gem04",
			"v12", "v13", "v14", "v15", "v16", "v17", "v18",
			"fpls", "wcrate", "scrate", "barb", "sbag",
		};
		
		Dictionary< string, Pair<byte,byte> > resourceMapping = new Dictionary<string, Pair<byte, byte>>() {
			{ "gold01", new Pair<byte,byte>(1,0) },
			{ "gold02", new Pair<byte,byte>(1,1) },
			{ "gold03", new Pair<byte,byte>(1,2) },
			{ "gold04", new Pair<byte,byte>(1,3) },
			
			{ "gem01", new Pair<byte,byte>(2,0) },
			{ "gem02", new Pair<byte,byte>(2,1) },
			{ "gem03", new Pair<byte,byte>(2,2) },
			{ "gem04", new Pair<byte,byte>(2,3) },
			
			// TODO Add cnc tiberium
		};
		
		
		public MapConverter(string filename)
		{						
			Map.Author = "Westwood Studios";
			
			IniFile file = new IniFile(FileSystem.Open(filename));
			
			IniSection basic = file.GetSection("Basic");
			Map.Title = basic.GetValue("Name", "(null)");
			
			
			INIFormat = int.Parse(basic.GetValue("NewINIFormat", "0"));

			IniSection map = file.GetSection("Map");
			Map.Tileset = Truncate(map.GetValue("Theater", "TEMPERAT"), 8);

			XOffset = int.Parse(map.GetValue("X", "0"));
			YOffset = int.Parse(map.GetValue("Y", "0"));

			Width = int.Parse(map.GetValue("Width", "0"));
			Height = int.Parse(map.GetValue("Height", "0"));
			MapSize = (INIFormat == 3) ? 128 : 64;
			
			Map.Size.X = MapSize;
			Map.Size.Y = MapSize;
			Map.Bounds = new int[] {XOffset, YOffset, Width, Height};
			
			if (INIFormat == 3) // RA map
			{
				UnpackRATileData(ReadPackedSection(file.GetSection("MapPack")));
				UnpackRAOverlayData(ReadPackedSection(file.GetSection("OverlayPack")));
				ReadRATrees(file);
			}
			else // CNC
			{
				UnpackCncTileData(FileSystem.Open(filename.Substring(0,filename.Length-4)+".bin"));
				ReadCncOverlay(file);
				ReadCncTrees(file);	
			}
			
			LoadActors(file, "STRUCTURES");
			LoadActors(file, "UNITS");
			LoadActors(file, "INFANTRY");
		
			var wp = file.GetSection("Waypoints")
					.Where(kv => int.Parse(kv.Value) > 0)
					.Select(kv => Pair.New(int.Parse(kv.Key), new int2(int.Parse(kv.Value) % MapSize, int.Parse(kv.Value) / MapSize)))
					.Where(a => a.First < 8)
					.ToArray();
			
			Map.PlayerCount = wp.Count();
			
			foreach (var kv in wp)
				Map.Waypoints.Add("spawn"+kv.First, kv.Second);
		}

		static MemoryStream ReadPackedSection(IniSection mapPackSection)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 1; ; i++)
			{
				string line = mapPackSection.GetValue(i.ToString(), null);
				if (line == null)
					break;

				sb.Append(line.Trim());
			}

			byte[] data = Convert.FromBase64String(sb.ToString());
			List<byte[]> chunks = new List<byte[]>();
			BinaryReader reader = new BinaryReader(new MemoryStream(data));

			try
			{
				while (true)
				{
					uint length = reader.ReadUInt32() & 0xdfffffff;
					byte[] dest = new byte[8192];
					byte[] src = reader.ReadBytes((int)length);

					/*int actualLength =*/ Format80.DecodeInto(src, dest);

					chunks.Add(dest);
				}
			}
			catch (EndOfStreamException) { }

			MemoryStream ms = new MemoryStream();
			foreach (byte[] chunk in chunks)
				ms.Write(chunk, 0, chunk.Length);

			ms.Position = 0;

			return ms;
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

		void UnpackRATileData( MemoryStream ms )
		{
			Map.MapTiles = new NewTileReference<ushort, byte>[ MapSize, MapSize ];
			for( int i = 0 ; i < MapSize ; i++ )
				for( int j = 0 ; j < MapSize ; j++ )
					Map.MapTiles[j, i] = new NewTileReference<ushort,byte>();
			
			for( int i = 0 ; i < MapSize ; i++ )
				for( int j = 0 ; j < MapSize ; j++ )
					Map.MapTiles[j, i].type = ReadWord(ms);

			for( int i = 0 ; i < MapSize ; i++ )
				for( int j = 0 ; j < MapSize ; j++ )
				{
					Map.MapTiles[j, i].index = (byte)ms.ReadByte();
					if( Map.MapTiles[ j, i ].type == 0xff || Map.MapTiles[ j, i ].type == 0xffff )
						Map.MapTiles[ j, i ].index = byte.MaxValue;
				}
		}
	
		void UnpackRAOverlayData( MemoryStream ms )
		{
			Map.MapResources = new NewTileReference<byte, byte>[ MapSize, MapSize ];		
			for( int i = 0 ; i < MapSize ; i++ )
				for( int j = 0 ; j < MapSize ; j++ )
				{
					byte o = ReadByte( ms );
					var res = Pair.New((byte)0,(byte)0);
					
					if (o != 255 && resourceMapping.ContainsKey(raOverlayNames[o]))
						res = resourceMapping[raOverlayNames[o]];
				
					Map.MapResources[j, i] = new NewTileReference<byte,byte>(res.First, res.Second);
				}
		}

		void ReadRATrees( IniFile file )
		{
			IniSection terrain = file.GetSection( "TERRAIN", true );
			if( terrain == null )
				return;
			int a = 0;
			foreach( KeyValuePair<string, string> kv in terrain )
			{
				var loc = int.Parse( kv.Key );
				Map.Actors.Add("Actor"+a++, new ActorReference(kv.Value, new int2(loc % MapSize, loc / MapSize), "Neutral" ) );
			}
		}
		
		void UnpackCncTileData( Stream ms )
		{		
			/*for( int i = 0 ; i < MapSize ; i++ )
				for( int j = 0 ; j < MapSize ; j++ )
				{
					MapTiles[j, i].tile = (byte)ms.ReadByte();	
					MapTiles[j, i].image = (byte)ms.ReadByte();
				
					if( MapTiles[ j, i ].tile == 0xff )
						MapTiles[ j, i ].image = (byte)( i % 4 + ( j % 4 ) * 4 );
				}*/
		}
		
		void ReadCncOverlay( IniFile file )
		{
			/*IniSection overlay = file.GetSection( "OVERLAY", true );
			if( overlay == null )
				return;

			foreach( KeyValuePair<string, string> kv in overlay )
			{
				var loc = int.Parse( kv.Key );
				int2 cell = new int2(loc % MapSize, loc / MapSize);
				MapTiles[ cell.X, cell.Y ].overlay = kv.Value.ToLower();
			}*/
		}
		
		
		void ReadCncTrees( IniFile file )
		{
			IniSection terrain = file.GetSection( "TERRAIN", true );
			if( terrain == null )
				return;
			
			int a = 0;
			foreach( KeyValuePair<string, string> kv in terrain )
			{
				var loc = int.Parse( kv.Key );
				Map.Actors.Add("Actor"+a++, new ActorReference( kv.Value.Split(',')[0], new int2(loc % MapSize, loc / MapSize),"Neutral"));
			}
		}
		
		void LoadActors(IniFile file, string section)
		{
			int a = 0;
			foreach (var s in file.GetSection(section, true))
			{
				//num=owner,type,health,location,facing,...
				var parts = s.Value.Split( ',' );
				var loc = int.Parse(parts[3]);
				if (parts[0] == "")
					parts[0] = "Neutral";
				Map.Actors.Add("Actor"+a++, new ActorReference( parts[1].ToLowerInvariant(), new int2(loc % MapSize, loc / MapSize), parts[0]));
			}
		}
		
		public void Save(string filepath)
		{
			Map.Tiledata = filepath+".bin";
			Map.Save(filepath+".yaml");
		}
	}
}
