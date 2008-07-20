using System;
using System.Collections.Generic;
using OpenRa.FileFormats;

using OpenRa.Game.Graphics;

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
		public Dictionary<string, Func<int2, Player, Building>> buildingCreation = new Dictionary<string, Func<int2, Player, Building>>();

		public Game(string mapName, Renderer renderer, int2 clientSize)
		{
			for (int i = 0; i < 8; i++)
				players.Add(i, new Player(i, string.Format("Multi{0}", i), OpenRa.TechTree.Race.Soviet));

			map = new Map(new IniFile(FileSystem.Open(mapName)));
			FileSystem.Mount(new Package(map.Theater + ".mix"));

			viewport = new Viewport(clientSize, map.Size, renderer);

			terrain = new TerrainRenderer(renderer, map, viewport);
			world = new World(renderer, this);
			treeCache = new TreeCache(map);

			foreach (TreeReference treeReference in map.Trees)
				world.Add(new Tree(treeReference, treeCache, map, this));

			pathFinder = new PathFinder(map, terrain.tileSet);

			network = new Network();

			buildingCreation.Add("proc", (location, owner) => new Refinery(location, owner, this));

			string[] buildings = { "fact", "powr", "apwr", "weap", "barr", "atek", "stek", "dome" };
			foreach (string s in buildings)
				AddBuilding(s);
		}

		void AddBuilding(string name)
		{
			buildingCreation.Add(name, (location, owner) => new Building(name, location, owner, this));
		}

		public void Tick()
		{
			viewport.DrawRegions(this);
			Queue<Packet> stuffFromOtherPlayers = network.Tick();	// todo: actually use the orders!
		}

		public void Issue(IOrder order) { order.Apply(this); }
	}
}
