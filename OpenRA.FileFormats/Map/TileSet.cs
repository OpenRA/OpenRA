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

		public readonly Walkability Walkability;
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

		public TileSet( string tilesetFile, string templatesFile, string suffix )
		{
			Walkability = new Walkability(templatesFile);
			char tileSetChar = char.ToUpperInvariant( suffix[ 0 ] );
			StreamReader tileIdFile = new StreamReader( FileSystem.Open(tilesetFile) );

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
						walk.Add((ushort)(start + i), Walkability.GetTileTemplate(tilename));

					using( Stream s = FileSystem.Open( tilename + "." + suffix ) )
					{
						if( !tiles.ContainsKey( (ushort)( start + i ) ) )
							tiles.Add( (ushort)( start + i ), new Terrain( s ) );
					}
				}
			}

			tileIdFile.Close();
			Convert("tileset-"+suffix+".yaml");

		}

		static List<string> SimpleFields = new List<string>() {
			"Name", "Size", "PickAny", "Bridge", "HP"
		};
		
		public void Convert(string outFile)
		{
			Dictionary<string, MiniYaml> root = new Dictionary<string, MiniYaml>();
			
			foreach(var w in walk)
			{
				Dictionary<string, MiniYaml> nodeYaml = new Dictionary<string, MiniYaml>();
				nodeYaml.Add("Id", new MiniYaml(w.Key.ToString(), null));

				foreach (var field in SimpleFields)
				{
					var save = field;
					System.Reflection.FieldInfo f = w.Value.GetType().GetField(field);
					if (f.GetValue(w.Value) == null) continue;
					
					if (field == "Name")
						save = "Image";
					
					if (field == "HP" && w.Value.HP == 0)
						continue;
					
					if (field == "HP")
						save = "Strength";
					
					if (field == "PickAny" && !w.Value.PickAny)
						continue;
					
					nodeYaml.Add(save, new MiniYaml(FieldSaver.FormatValue(w.Value, f), null));
				}
				
				nodeYaml.Add("Tiles", MiniYaml.FromDictionary<int, TerrainType>(w.Value.TerrainType));

								
				root.Add("TileTemplate@{0}".F(w.Key), new MiniYaml(null, nodeYaml));
			}
			root.WriteToFile(outFile);
		}
		
		public byte[] GetBytes(TileReference<ushort,byte> r)
		{
			Terrain tile;
			if( tiles.TryGetValue( r.type, out tile ) )
				return tile.TileBitmapBytes[ r.image ];
			
			byte[] missingTile = new byte[ 24 * 24 ];
			for( int i = 0 ; i < missingTile.Length ; i++ )
				missingTile[ i ] = 0x36;

			return missingTile;
		}

		public TerrainType GetTerrainType(TileReference<ushort, byte> r)
		{
			var tt = walk[r.type].TerrainType;
			TerrainType ret;
			if (!tt.TryGetValue(r.image, out ret))
				return 0;// Default zero (walkable)
			return ret;
		}
	}
}
