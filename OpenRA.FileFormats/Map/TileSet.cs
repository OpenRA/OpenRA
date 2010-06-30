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
using System.Drawing;

namespace OpenRA.FileFormats
{
	public class TerrainTypeInfo
	{
		public string Type;
		public bool Buildable = true;
		public bool AcceptSmudge = true;
		public bool IsWater = false;
		public Color Color;
		
		public TerrainTypeInfo(MiniYaml my)
		{
			FieldLoader.Load(this, my);
		}
	}
	
	public class TileTemplate
	{
		public ushort Id;
		public string Image;
		public int2 Size;
		public bool PickAny;
		public Dictionary<byte, string> Tiles = new Dictionary<byte, string>();
		
		static List<string> fields = new List<string>() {"Id", "Image", "Size", "PickAny"};

		public TileTemplate(Dictionary<string,MiniYaml> my)
		{
			FieldLoader.LoadFields(this, my, fields);
			foreach (var tt in my["Tiles"].Nodes)
				Tiles.Add(byte.Parse(tt.Key), tt.Value.Value);
		}
	}
	
	public class TileSet
	{
		public readonly string Name;
		public readonly string Id;
		public readonly string[] Extensions;
		public readonly Dictionary<string, TerrainTypeInfo> Terrain = new Dictionary<string, TerrainTypeInfo>();
		public readonly Dictionary<ushort, Terrain> Tiles = new Dictionary<ushort, Terrain>();
		public readonly Dictionary<ushort, TileTemplate> Templates = new Dictionary<ushort, TileTemplate>();
		
		public TileSet( string filepath )
		{
			var yaml = MiniYaml.FromFile(filepath);
			
			// General info
			FieldLoader.Load(this, yaml["General"]);

			// TerrainTypes
			foreach (var tt in yaml["Terrain"].Nodes)
			{
				var t = new TerrainTypeInfo(tt.Value);
				Terrain.Add(t.Type, t);
			}
			
			// Templates
			foreach (var tt in yaml["Templates"].Nodes)
			{
				var t = new TileTemplate(tt.Value.Nodes);
				Templates.Add(t.Id, t);
			}
		}
		
		public void LoadTiles()
		{
			foreach (var t in Templates)
				using( Stream s = FileSystem.OpenWithExts(t.Value.Image, Extensions) )
				{
					if( !Tiles.ContainsKey( t.Key ) )
						Tiles.Add( t.Key, new Terrain( s ) );
				}
		}
				
		public byte[] GetBytes(TileReference<ushort,byte> r)
		{
			Terrain tile;
			if( Tiles.TryGetValue( r.type, out tile ) )
				return tile.TileBitmapBytes[ r.image ];
			
			byte[] missingTile = new byte[ 24 * 24 ];
			for( int i = 0 ; i < missingTile.Length ; i++ )
				missingTile[ i ] = 0x36;

			return missingTile;
		}

		public string GetTerrainType(TileReference<ushort, byte> r)
		{
			var tt = Templates[r.type].Tiles;
			string ret;
			if (!tt.TryGetValue(r.image, out ret))
				return "Clear"; // Default walkable
			return ret;
		}
	}
}
