#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenRA.FileFormats
{
	public class TerrainTypeInfo
	{
		public string Type;
		public bool Buildable = true;
		public bool AcceptSmudge = true;
		public bool IsWater = false;
		public Color Color;

		public TerrainTypeInfo() {}
		public TerrainTypeInfo(MiniYaml my) { FieldLoader.Load(this, my); }
		public MiniYaml Save() { return FieldSaver.Save(this); }
	}
	
	[FieldLoader.Foo("Id", "Image", "Size", "PickAny")]
	public class TileTemplate
	{
		public ushort Id;
		public string Image;
		public int2 Size;
		public bool PickAny;

		[FieldLoader.LoadUsing( "LoadTiles" )]
		public Dictionary<byte, string> Tiles = new Dictionary<byte, string>();
		
		public TileTemplate() {}
		public TileTemplate(MiniYaml my)
		{
			FieldLoader.Load( this, my );
		}

		static object LoadTiles( MiniYaml y )
		{
			return y.NodesDict["Tiles"].NodesDict.ToDictionary(
				t => byte.Parse(t.Key),
				t => t.Value.Value );
		}
		
		public MiniYaml Save()
		{
			var root = new List<MiniYamlNode>();
			foreach (var field in new string[] {"Id", "Image", "Size", "PickAny"})
			{
				FieldInfo f = this.GetType().GetField(field);
				if (f.GetValue(this) == null) continue;
				root.Add( new MiniYamlNode( field, FieldSaver.FormatValue( this, f ) ) );
			}

			root.Add( new MiniYamlNode( "Tiles", null,
				Tiles.Select( x => new MiniYamlNode( x.Key.ToString(), x.Value ) ).ToList() ) );
			
			return new MiniYaml(null, root);
		}
	}
	
	public class TileSet
	{
		public string Name;
		public string Id;
		public string Palette;
		public string[] Extensions;
		public Dictionary<string, TerrainTypeInfo> Terrain = new Dictionary<string, TerrainTypeInfo>();
		public Dictionary<ushort, Terrain> Tiles = new Dictionary<ushort, Terrain>();
		public Dictionary<ushort, TileTemplate> Templates = new Dictionary<ushort, TileTemplate>();
		static List<string> fields = new List<string>() {"Name", "Id", "Palette", "Extensions"};

		public TileSet() {}
		public TileSet( string filepath )
		{
			var yaml = MiniYaml.FromFile(filepath).ToDictionary( x => x.Key, x => x.Value );
			
			// General info
			FieldLoader.Load(this, yaml["General"]);

			// TerrainTypes
			Terrain = yaml["Terrain"].NodesDict.Values
				.Select(y => new TerrainTypeInfo(y)).ToDictionary(t => t.Type);

			// Templates
			Templates = yaml["Templates"].NodesDict.Values
				.Select(y => new TileTemplate(y)).ToDictionary(t => t.Id);
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
		
		public void Save(string filepath)
		{			
			var root = new List<MiniYamlNode>();
			foreach (var field in fields)
			{
				FieldInfo f = this.GetType().GetField(field);
				if (f.GetValue(this) == null) continue;
				root.Add( new MiniYamlNode( field, FieldSaver.FormatValue( this, f ) ) );
			}
			
			var gen = new List<MiniYamlNode>();
			foreach (var field in fields)
			{
				FieldInfo f = this.GetType().GetField(field);
				if (f.GetValue(this) == null) continue;
				gen.Add( new MiniYamlNode( field, FieldSaver.FormatValue( this, f ) ) );
			}
			root.Add( new MiniYamlNode( "General", null, gen ) );

			root.Add( new MiniYamlNode( "Terrain", null, 
				Terrain.Select( t => new MiniYamlNode(
					"TerrainType@{0}".F( t.Value.Type ),
					t.Value.Save() ) ).ToList() ) );

			root.Add( new MiniYamlNode( "Templates", null,
				Templates.Select( t => new MiniYamlNode(
					"Template@{0}".F( t.Value.Id ),
					t.Value.Save() ) ).ToList() ) );
			root.WriteToFile(filepath);
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
