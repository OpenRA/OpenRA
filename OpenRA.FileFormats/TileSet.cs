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

using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OpenRA.FileFormats
{
	public class TileSet
	{
		public readonly Dictionary<ushort, Terrain> tiles = new Dictionary<ushort, Terrain>();

		public readonly Walkability Walkability = new Walkability();
		public readonly Dictionary<ushort, TileTemplate> walk 
			= new Dictionary<ushort, TileTemplate>();

		string NextLine( StreamReader reader )
		{
			string ret;
			do
			{
				ret = reader.ReadLine();
				if( ret == null )
					return null;
				ret = ret.Trim();
			}
			while( ret.Length == 0 || ret[ 0 ] == ';' );
			return ret;
		}

		public TileSet( string suffix )
		{
			Walkability = new Walkability();

			char tileSetChar = char.ToUpperInvariant( suffix[ 1 ] );
			StreamReader tileIdFile = new StreamReader( FileSystem.Open( "tileSet.til" ) );

			while( true )
			{
				string tileSetStr = NextLine( tileIdFile );
				string countStr = NextLine( tileIdFile );
				string startStr = NextLine( tileIdFile );
				string pattern = NextLine( tileIdFile );
				if( tileSetStr == null || countStr == null || startStr == null || pattern == null )
					break;

				if( tileSetStr.IndexOf( tileSetChar.ToString() ) == -1 )
					continue;

				int count = int.Parse( countStr );
				int start = int.Parse( startStr, NumberStyles.HexNumber );
				for( int i = 0 ; i < count ; i++ )
				{
					string tilename = string.Format(pattern, i + 1);

					if (!walk.ContainsKey((ushort)(start + i)))
						walk.Add((ushort)(start + i), Walkability.GetWalkability(tilename));

					using( Stream s = FileSystem.Open( tilename + suffix ) )
					{
						Log.Write(tilename+suffix);
						if( !tiles.ContainsKey( (ushort)( start + i ) ) )
							tiles.Add( (ushort)( start + i ), new Terrain( s ) );
					}
				}
			}

			tileIdFile.Close();
		}

		public byte[] GetBytes(TileReference r)
		{
			Terrain tile;

			if( tiles.TryGetValue( r.tile, out tile ) )
				return tile.TileBitmapBytes[ r.image ];
			
			byte[] missingTile = new byte[ 24 * 24 ];
			for( int i = 0 ; i < missingTile.Length ; i++ )
				missingTile[ i ] = 0x36;

			return missingTile;
		}

		public int GetWalkability(TileReference r)
		{
			if (r.tile == 0xff || r.tile == 0xffff)
				r.image = 0;
			
			try {
				return walk[r.tile].TerrainType[r.image];
			}
			catch (KeyNotFoundException)
			{
				return 0; // Default zero (walkable)
			}
			
		}
	}
}
