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
		public readonly TechTree.TechTree techTree = new TechTree.TechTree();

		// temporary, until we remove all the subclasses of Building
		public Dictionary<string, Provider<Building, int2, int>> buildingCreation = new Dictionary<string, Provider<Building, int2, int>>();

		public Game(string mapName, Renderer renderer, int2 clientSize)
		{
			map = new Map(new IniFile(FileSystem.Open(mapName)));
			FileSystem.Mount(new Package("../../../" + map.Theater + ".mix"));

			viewport = new Viewport(clientSize, map.Size, renderer);

			terrain = new TerrainRenderer(renderer, map, viewport);
			world = new World(renderer, viewport);
			treeCache = new TreeCache(renderer.Device, map);

			foreach (TreeReference treeReference in map.Trees)
				world.Add(new Tree(treeReference, treeCache, map));

			pathFinder = new PathFinder(map, terrain.tileSet);

			network = new Network();

			buildingCreation.Add( "fact",
				delegate( int2 location, int palette )
				{
					return new ConstructionYard( location, palette );
				} );
			buildingCreation.Add( "proc",
				delegate( int2 location, int palette )
				{
					return new Refinery( location, palette );
				} );

			string[] buildings = { "powr", "apwr", "weap", "barr", "atek", "stek", "dome" };
			foreach (string s in buildings)
				AddBuilding(s);
		}

		void AddBuilding(string name)
		{
			buildingCreation.Add(name,
				delegate(int2 location, int palette)
				{
					return new Building(name, location, palette);
				});
		}

		public void Tick()
		{
			viewport.DrawRegions(this);
		}

		public void Issue(IOrder order)
		{
			order.Apply( this );
		}
	}
}
