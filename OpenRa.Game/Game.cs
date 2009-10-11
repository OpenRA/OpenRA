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
		readonly TreeCache treeCache;
		public readonly TerrainRenderer terrain;
		public readonly Viewport viewport;
		public readonly PathFinder pathFinder;
		public readonly Network network;
		public readonly WorldRenderer worldRenderer;
		public readonly Controller controller;

		int localPlayerIndex = 2;

		public readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

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

			foreach( TreeReference treeReference in map.Trees )
				world.Add( new Actor( treeReference, treeCache, map.Offset ) );

			pathFinder = new PathFinder(map, terrain.tileSet);

			network = new Network();

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
