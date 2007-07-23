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

		readonly Dictionary<ushort, Dictionary<int, int>> walk = 
			new Dictionary<ushort, Dictionary<int, int>>();	// cjf will fix

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
			Walkability walkability = new Walkability();

			char tileSetChar = char.ToUpperInvariant( suffix[ 1 ] );
			StreamReader tileIdFile = File.OpenText( "../../../tileSet.til" );

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
						walk.Add((ushort)(start + i), walkability.GetWalkability(tilename));

					using( Stream s = FileSystem.Open( tilename + suffix ) )
					{
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

			return walk[r.tile][r.image];
		}
	}
}
