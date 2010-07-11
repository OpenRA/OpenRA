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
using System.Drawing;
using System.Drawing.Imaging;
using OpenRA;
using OpenRA.FileFormats;

namespace MapConverter
{
	public class MapConverter
	{
		// Mapping from ra overlay index to type string
		static string[] raOverlayNames =
		{
			"sbag", "cycl", "brik", "fenc", "wood",
			"gold01", "gold02", "gold03", "gold04",
			"gem01", "gem02", "gem03", "gem04",
			"v12", "v13", "v14", "v15", "v16", "v17", "v18",
			"fpls", "wcrate", "scrate", "barb", "sbag",
		};
		
		static Dictionary< string, Pair<byte,byte> > overlayResourceMapping = new Dictionary<string, Pair<byte, byte>>()
		{
			// RA Gems, Gold
			{ "gold01", new Pair<byte,byte>(1,0) },
			{ "gold02", new Pair<byte,byte>(1,1) },
			{ "gold03", new Pair<byte,byte>(1,2) },
			{ "gold04", new Pair<byte,byte>(1,3) },
			
			{ "gem01", new Pair<byte,byte>(2,0) },
			{ "gem02", new Pair<byte,byte>(2,1) },
			{ "gem03", new Pair<byte,byte>(2,2) },
			{ "gem04", new Pair<byte,byte>(2,3) },
			
			// cnc tiberium
			{ "ti1", new Pair<byte,byte>(1,0) },
			{ "ti2", new Pair<byte,byte>(1,1) },
			{ "ti3", new Pair<byte,byte>(1,2) },
			{ "ti4", new Pair<byte,byte>(1,3) },
			{ "ti5", new Pair<byte,byte>(1,4) },
			{ "ti6", new Pair<byte,byte>(1,5) },
			{ "ti7", new Pair<byte,byte>(1,6) },
			{ "ti8", new Pair<byte,byte>(1,7) },
			{ "ti9", new Pair<byte,byte>(1,8) },
			{ "ti10", new Pair<byte,byte>(1,9) },
			{ "ti11", new Pair<byte,byte>(1,10) },
			{ "ti12", new Pair<byte,byte>(1,11) },
		};
		
		static Dictionary<string, string> overlayActorMapping = new Dictionary<string,string>() {
			// Fences
			{"sbag","sbag"},
			{"cycl","cycl"},
			{"brik","brik"},
			{"fenc","fenc"},
			{"wood","wood"},
			
			// Fields
			{"v12","v12"},
			{"v13","v13"},
			{"v14","v14"},
			{"v15","v15"},
			{"v16","v16"},
			{"v17","v17"},
			{"v18","v18"},
			
			// Crates
			{"wcrate","crate"},
			{"scrate","crate"},
		};
		
		int MapSize;
		int ActorCount = 0; 
		Map Map = new Map();
		Manifest manifest;

		public MapConverter(string[] args)
		{
			if (args.Length != 3)
			{
				Console.WriteLine("usage: MapConverter mod[,mod]* input-map.ini output-map.yaml");
				return;
			}

			var mods = args[0].Split(',');
			manifest = new Manifest(mods);

			foreach (var folder in manifest.Folders) FileSystem.Mount(folder);
			foreach (var pkg in manifest.Packages) FileSystem.Mount(pkg);
			
			ConvertIniMap(args[1]);
			Save(args[2]);
		}
		
		
		static Dictionary<Pair<string,string>,Pair<string,string> > fileMapping = new Dictionary<Pair<string,string>,Pair<string,string> >()
		{
			{Pair.New("ra","TEMPERAT"),Pair.New("tem","temperat.col")},
			{Pair.New("ra","SNOW"),Pair.New("sno","snow.col")},
			{Pair.New("ra","INTERIOR"),Pair.New("int","temperat.col")},
			{Pair.New("cnc","DESERT"),Pair.New("des","desert.col")},
			{Pair.New("cnc","TEMPERAT"),Pair.New("tem","temperat.col")},
			{Pair.New("cnc","WINTER"),Pair.New("win","winter.col")},
		};
		
		public void ConvertIniMap(string iniFile)
		{
			IniFile file = new IniFile(FileSystem.Open(iniFile));
			IniSection basic = file.GetSection("Basic");
			IniSection map = file.GetSection("Map");
			var INIFormat = int.Parse(basic.GetValue("NewINIFormat", "0"));
			var XOffset = int.Parse(map.GetValue("X", "0"));
			var YOffset = int.Parse(map.GetValue("Y", "0"));
			var Width = int.Parse(map.GetValue("Width", "0"));
			var Height = int.Parse(map.GetValue("Height", "0"));
			MapSize = (INIFormat == 3) ? 128 : 64;
			
			Map.Title = basic.GetValue("Name", "(null)");
			Map.Author = "Westwood Studios";
			Map.Tileset = Truncate(map.GetValue("Theater", "TEMPERAT"), 8);
			Map.MapSize.X = MapSize;
			Map.MapSize.Y = MapSize;
			Map.TopLeft = new int2 (XOffset, YOffset);
			Map.BottomRight = new int2(XOffset+Width,YOffset+Height);
			Map.Selectable = true;
			
			if (INIFormat == 3) // RA map
			{
				UnpackRATileData(ReadPackedSection(file.GetSection("MapPack")));
				UnpackRAOverlayData(ReadPackedSection(file.GetSection("OverlayPack")));
				ReadRATrees(file);
				// TODO: Fixme
				//tileset = new TileSet("tileSet.til","templates.ini",fileMapping[Pair.New("ra",Map.Tileset)].First);
			}
			else // CNC
			{
				UnpackCncTileData(FileSystem.Open(iniFile.Substring(0,iniFile.Length-4)+".bin"));
				ReadCncOverlay(file);
				ReadCncTrees(file);
				
				// TODO: Fixme
				//tileset = new TileSet("tileSet.til","templates.ini",fileMapping[Pair.New("cnc",Map.Tileset)].First);
			}
			
			LoadActors(file, "STRUCTURES");
			LoadActors(file, "UNITS");
			LoadActors(file, "INFANTRY");
			LoadSmudges(file, "SMUDGE");
		
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
			Map.MapTiles = new TileReference<ushort, byte>[ MapSize, MapSize ];
			for( int i = 0 ; i < MapSize ; i++ )
				for( int j = 0 ; j < MapSize ; j++ )
					Map.MapTiles[i,j] = new TileReference<ushort,byte>();
			
			for( int j = 0 ; j < MapSize ; j++ )
				for( int i = 0 ; i < MapSize ; i++ )
					Map.MapTiles[i,j].type = ReadWord(ms);

			for( int j = 0 ; j < MapSize ; j++ )
				for( int i = 0 ; i < MapSize ; i++ )
				{
					Map.MapTiles[i,j].index = ReadByte(ms);
					if( Map.MapTiles[i,j].type == 0xff || Map.MapTiles[i,j].type == 0xffff )
						Map.MapTiles[i,j].index = byte.MaxValue;
				}
		}
	
		void UnpackRAOverlayData( MemoryStream ms )
		{
			Map.MapResources = new TileReference<byte, byte>[ MapSize, MapSize ];		
			for( int j = 0 ; j < MapSize ; j++ )
				for( int i = 0 ; i < MapSize ; i++ )
				{
					byte o = ReadByte( ms );
					var res = Pair.New((byte)0,(byte)0);
					
					if (o != 255 && overlayResourceMapping.ContainsKey(raOverlayNames[o]))
						res = overlayResourceMapping[raOverlayNames[o]];
					
					Map.MapResources[i,j] = new TileReference<byte,byte>(res.First, res.Second);
				
					if (o != 255 && overlayActorMapping.ContainsKey(raOverlayNames[o]))
							Map.Actors.Add("Actor"+ActorCount, new ActorReference("Actor"+ActorCount++, overlayActorMapping[raOverlayNames[o]], new int2(i,j), "Neutral"));
				}
		}

		void ReadRATrees( IniFile file )
		{
			IniSection terrain = file.GetSection( "TERRAIN", true );
			if( terrain == null )
				return;
			
			foreach( KeyValuePair<string, string> kv in terrain )
			{
				var loc = int.Parse( kv.Key );
				Map.Actors.Add("Actor"+ActorCount, new ActorReference("Actor"+ActorCount++,kv.Value.ToLowerInvariant(), new int2(loc % MapSize, loc / MapSize), "Neutral" ) );
			}
		}
		
		void UnpackCncTileData( Stream ms )
		{		
			Map.MapTiles = new TileReference<ushort, byte>[ MapSize, MapSize ];
			for( int i = 0 ; i < MapSize ; i++ )
				for( int j = 0 ; j < MapSize ; j++ )
					Map.MapTiles[i,j] = new TileReference<ushort,byte>();
			
			for( int j = 0 ; j < MapSize ; j++ )
				for( int i = 0 ; i < MapSize ; i++ )
				{
					Map.MapTiles[i,j].type = ReadByte(ms);
					Map.MapTiles[i,j].index = ReadByte(ms);
					
					if( Map.MapTiles[i,j].type == 0xff )
						Map.MapTiles[i,j].index = byte.MaxValue;
				}			
		}
				
		void ReadCncOverlay( IniFile file )
		{
			IniSection overlay = file.GetSection( "OVERLAY", true );
			if( overlay == null )
				return;
		
			Map.MapResources = new TileReference<byte, byte>[ MapSize, MapSize ];		
			foreach( KeyValuePair<string, string> kv in overlay )
			{
				var loc = int.Parse( kv.Key );
				int2 cell = new int2(loc % MapSize, loc / MapSize);
				
				var res = Pair.New((byte)0,(byte)0);
				if (overlayResourceMapping.ContainsKey(kv.Value.ToLower()))
						res = overlayResourceMapping[kv.Value.ToLower()];
				
				Map.MapResources[ cell.X, cell.Y ] = new TileReference<byte,byte>(res.First, res.Second);
				
				if (overlayActorMapping.ContainsKey(kv.Value.ToLower()))
					Map.Actors.Add("Actor"+ActorCount, new ActorReference("Actor"+ActorCount++, overlayActorMapping[kv.Value.ToLower()], new int2(cell.X,cell.Y), "Neutral"));
			}
		}
		
		
		void ReadCncTrees( IniFile file )
		{
			IniSection terrain = file.GetSection( "TERRAIN", true );
			if( terrain == null )
				return;

			foreach( KeyValuePair<string, string> kv in terrain )
			{
				var loc = int.Parse( kv.Key );
				Map.Actors.Add("Actor"+ActorCount, new ActorReference("Actor"+ActorCount++, kv.Value.Split(',')[0].ToLowerInvariant(), new int2(loc % MapSize, loc / MapSize),"Neutral"));
			}
		}
		
		void LoadActors(IniFile file, string section)
		{
			foreach (var s in file.GetSection(section, true))
			{
				//num=owner,type,health,location,facing,...
				var parts = s.Value.Split( ',' );
				var loc = int.Parse(parts[3]);
				if (parts[0] == "")
					parts[0] = "Neutral";
				Map.Actors.Add("Actor"+ActorCount, new ActorReference("Actor"+ActorCount++, parts[1].ToLowerInvariant(), new int2(loc % MapSize, loc / MapSize), parts[0]));
			}
		}
		
		void LoadSmudges(IniFile file, string section)
		{
			foreach (var s in file.GetSection(section, true))
			{
				//loc=type,loc,depth
				var parts = s.Value.Split( ',' );
				var loc = int.Parse(parts[1]);
				Map.Smudges.Add(new SmudgeReference(parts[0].ToLowerInvariant(),new int2(loc % MapSize, loc / MapSize),int.Parse(parts[2])));
			}
		}
		static string Truncate( string s, int maxLength )
		{
			return s.Length <= maxLength ? s : s.Substring(0,maxLength );
		}
				
		public void Save(string filepath)
		{
			Directory.CreateDirectory(filepath);
			
			Map.Package = new Folder(filepath);
			Map.Save(filepath);
		}
	}
}
