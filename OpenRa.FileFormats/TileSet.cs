using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

namespace OpenRa.FileFormats
{
	public class TileSet
	{
		public readonly Dictionary<ushort, Terrain> tiles = new Dictionary<ushort, Terrain>();
		public readonly Package MixFile;

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

		public TileSet( Package mixFile, string suffix )
		{
			char tileSetChar = char.ToUpperInvariant( suffix[ 1 ] );
			MixFile = mixFile;
			StreamReader tileIdFile = File.OpenText( "../../../tileSet.til" );

			while( true )
			{
				string tileSetStr = NextLine( tileIdFile );
				string countStr = NextLine( tileIdFile );
				string startStr = NextLine( tileIdFile );
				string pattern = NextLine( tileIdFile ) + suffix;
				if( tileSetStr == null || countStr == null || startStr == null || pattern == null )
					break;

				if( tileSetStr.IndexOf( tileSetChar.ToString() ) == -1 )
					continue;

				int count = int.Parse( countStr );
				int start = int.Parse( startStr, NumberStyles.HexNumber );
				for( int i = 0 ; i < count ; i++ )
				{
					Stream s = mixFile.GetContent(string.Format(pattern, i + 1));
					if (!tiles.ContainsKey((ushort)(start + i)))
						tiles.Add((ushort)(start + i), new Terrain(s));
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
	}
}
