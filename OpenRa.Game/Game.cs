using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	class Game
	{
		public readonly World world;
		public readonly Map map;
		public readonly TreeCache treeCache;
		public readonly TerrainRenderer terrain;
		public readonly Viewport viewport;
		public readonly PathFinder pathFinder;
		public readonly Network network;

		public Game( string mapName, Renderer renderer, int2 clientSize )
		{
			map = new Map( new IniFile( FileSystem.Open( mapName ) ) );
			FileSystem.Mount( new Package( "../../../" + map.Theater + ".mix" ) );

			viewport = new Viewport( clientSize, new float2( map.Size ), renderer );
			
			terrain = new TerrainRenderer( renderer, map, viewport );
			world = new World( renderer, viewport );
			treeCache = new TreeCache( renderer.Device, map );

			foreach( TreeReference treeReference in map.Trees )
				world.Add( new Tree( treeReference, treeCache, map ) );

			pathFinder = new PathFinder( map, terrain.tileSet );

			network = new Network();
		}

		public void Tick()
		{
			viewport.DrawRegions( this );
		}

		public void Issue( IOrder order )
		{
			order.Apply();
		}
	}
}
