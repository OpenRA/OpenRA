using System;
using System.Collections.Generic;
using OpenRa.FileFormats;
using System.Linq;

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
		public readonly WorldRenderer worldRenderer;
		public readonly Controller controller;

		int localPlayerIndex = 1;

		public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

		// temporary, until we remove all the subclasses of Building
        public Dictionary<string, Func<int2, Player, PlayerOwned>> buildingCreation;

		public Player LocalPlayer { get { return players[localPlayerIndex]; } }

		public Game(string mapName, Renderer renderer, int2 clientSize)
		{
			Rules.LoadRules();

			for( int i = 0 ; i < 8 ; i++ )
				players.Add(i, new Player(i, string.Format("Multi{0}", i), OpenRa.TechTree.Race.Soviet));

			map = new Map(new IniFile(FileSystem.Open(mapName)));
			FileSystem.Mount(new Package(map.Theater + ".mix"));

			viewport = new Viewport(clientSize, map.Size, renderer);

			terrain = new TerrainRenderer(renderer, map, viewport);
			world = new World(this);
			treeCache = new TreeCache(map);

			foreach (TreeReference treeReference in map.Trees)
				world.Add(new Tree(treeReference, treeCache, map, this));

			pathFinder = new PathFinder(map, terrain.tileSet);

			network = new Network();

            var buildings = new[] { "fact", "powr", "apwr", "barr", "atek", "stek", "dome" };
            buildingCreation = buildings.ToDictionary(s => s, 
                s => (Func<int2, Player, PlayerOwned>)(
                    (l, o) => new Building(s, l, o, this)));

            buildingCreation.Add("proc", (location, owner) => new Refinery(location, owner, this));
            buildingCreation.Add("weap", (location, owner) => new WarFactory(location, owner, this));
            buildingCreation.Add("3tnk", (location, owner) => new TurretedUnit("3tnk", location, owner, this));

			controller = new Controller(this);		// CAREFUL THERES AN UGLY HIDDEN DEPENDENCY HERE STILL
			worldRenderer = new WorldRenderer(renderer, world);
		}

		public void Tick()
		{
			var stuffFromOtherPlayers = network.Tick();	// todo: actually use the orders!
			world.Update();

			viewport.DrawRegions();
		}
	}
}
