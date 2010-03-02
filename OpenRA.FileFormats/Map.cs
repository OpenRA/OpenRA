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

namespace OpenRA.FileFormats
{
	public class Map
	{
		public readonly string BinaryPart;
		public readonly string Title;
		public readonly string Theater;

		public readonly int XOffset;
		public readonly int YOffset;
		public int2 Offset { get { return new int2( XOffset, YOffset ); } }

		public readonly int Width;
		public readonly int Height;
		public int2 Size { get { return new int2(Width, Height); } }

		public readonly TileReference[ , ] MapTiles = new TileReference[ 128, 128 ];
		public readonly List<TreeReference> Trees = new List<TreeReference>();

		public readonly IEnumerable<int2> SpawnPoints;

		static string Truncate( string s, int maxLength )
		{
			return s.Length <= maxLength ? s : s.Substring(0,maxLength );
		}

		public string TileSuffix { get { return "." + Truncate(Theater, 3); } }

		public Map(IniFile file)
		{
			for (int j = 0; j < 128; j++)
				for (int i = 0; i < 128; i++)
					MapTiles[i, j] = new TileReference();

			IniSection basic = file.GetSection("Basic");
			Title = basic.GetValue("Name", "(null)");
			BinaryPart = basic.GetValue("BinaryPart", "scm02ea.bin");
			IniSection map = file.GetSection("Map");
			Theater = Truncate(map.GetValue("Theater", "DESERT"), 8);

			XOffset = int.Parse(map.GetValue("X", "0"));
			YOffset = int.Parse(map.GetValue("Y", "0"));

			Width = int.Parse(map.GetValue("Width", "0"));
			Height = int.Parse(map.GetValue("Height", "0"));
			
			

			//UnpackTileData(ReadPackedSection(file.GetSection("MapPack")));
			//UnpackOverlayData(ReadPackedSection(file.GetSection("OverlayPack")));
			//ReadTrees(file);
			
			UnpackCncTileData(FileSystem.Open(BinaryPart));
			
			SpawnPoints = file.GetSection("Waypoints")
				.Select(kv => Pair.New(int.Parse(kv.Key), new int2(int.Parse(kv.Value) % 128, int.Parse(kv.Value) / 128)))
				.Where(a => a.First < 8)
				.Select(a => a.Second)
				.ToArray();
		}

		void UnpackCncTileData( Stream ms )
		{		
			for( int i = 0 ; i < 64 ; i++ )
				for( int j = 0 ; j < 64 ; j++ )
				{
					MapTiles[j, i].tile = (byte)ms.ReadByte();	
					MapTiles[j, i].image = (byte)ms.ReadByte();
				
					if( MapTiles[ j, i ].tile == 0xff )
						MapTiles[ j, i ].image = (byte)( i % 4 + ( j % 4 ) * 4 );
				}
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

		void UnpackTileData( MemoryStream ms )
		{
			for( int i = 0 ; i < 128 ; i++ )
				for( int j = 0 ; j < 128 ; j++ )
					MapTiles[j, i].tile = ReadWord(ms);

			for( int i = 0 ; i < 128 ; i++ )
				for( int j = 0 ; j < 128 ; j++ )
				{
					MapTiles[j, i].image = (byte)ms.ReadByte();
					if( MapTiles[ j, i ].tile == 0xff || MapTiles[ j, i ].tile == 0xffff )
						MapTiles[ j, i ].image = (byte)( i % 4 + ( j % 4 ) * 4 );
				}
		}

		void UnpackOverlayData( MemoryStream ms )
		{
			for( int i = 0 ; i < 128 ; i++ )
				for( int j = 0 ; j < 128 ; j++ )
					MapTiles[ j, i ].overlay = ReadByte( ms );
		}

		void ReadTrees( IniFile file )
		{
			IniSection terrain = file.GetSection( "TERRAIN", true );
			if( terrain == null )
				return;

			foreach( KeyValuePair<string, string> kv in terrain )
				Trees.Add( new TreeReference( int.Parse( kv.Key ), kv.Value ) );
		}

		public bool IsInMap(int x, int y)
		{
			return (x >= XOffset && y >= YOffset && x < XOffset + Width && y < YOffset + Height);
		}
	}
}
