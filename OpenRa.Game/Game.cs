using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using BluntDirectX.Direct3D;
using System.Drawing;

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

		public int localPlayerIndex = 1;

		public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

		// temporary, until we remove all the subclasses of Building
		public Dictionary<string, Provider<Building, int2, Player>> buildingCreation = new Dictionary<string, Provider<Building, int2, Player>>();

		public Game(string mapName, Renderer renderer, int2 clientSize)
		{
			for( int i = 0 ; i < 8 ; i++ )
				players.Add( i, new Player( i, string.Format( "Multi{0}", i ), OpenRa.TechTree.Race.Soviet ) );

			map = new Map(new IniFile(FileSystem.Open(mapName)));
			FileSystem.Mount(new Package("../../../" + map.Theater + ".mix"));

			viewport = new Viewport(clientSize, map.Size, renderer);

			terrain = new TerrainRenderer(renderer, map, viewport);
			world = new World(renderer, this);
			treeCache = new TreeCache(renderer.Device, map);

			foreach (TreeReference treeReference in map.Trees)
				world.Add(new Tree(treeReference, treeCache, map));

			pathFinder = new PathFinder(map, terrain.tileSet);

			network = new Network();

			buildingCreation.Add( "fact",
				delegate( int2 location, Player owner )
				{
					return new ConstructionYard( location, owner );
				} );
			buildingCreation.Add( "proc",
				delegate( int2 location, Player owner )
				{
					return new Refinery( location, owner );
				} );

			string[] buildings = { "powr", "apwr", "weap", "barr", "atek", "stek", "dome" };
			foreach (string s in buildings)
				AddBuilding(s);
		}

		void AddBuilding( string name )
		{
			buildingCreation.Add( name,
				delegate( int2 location, Player owner )
				{
					return new Building( name, location, owner );
				} );
		}

		public void Tick()
		{
			viewport.DrawRegions(this);
			Queue<Packet> stuffFromOtherPlayers = network.Tick();	// todo: actually use the orders!
		}

		public void Issue(IOrder order)
		{
			order.Apply( this );
		}
	}
}
