using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using IjwFramework.Collections;
using IjwFramework.Types;
using IrrKlang;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;
using OpenRa.Game.Support;
using System.Net.Sockets;
using System.Windows.Forms;

namespace OpenRa.Game
{
	static class Game
	{
		public static readonly int CellSize = 24;

		public static World world;
		public static Viewport viewport;
		public static PathFinder PathFinder;
		public static WorldRenderer worldRenderer;
		public static Controller controller;
		public static Chrome chrome;

		public static OrderManager orderManager;

		static int localPlayerIndex;

		public static Dictionary<int, Player> players = new Dictionary<int, Player>();

		public static Player LocalPlayer
		{
			get { return players[localPlayerIndex]; }
			set
			{
				localPlayerIndex = value.Index;
				viewport.GoToStartLocation();
			}
		}
		public static BuildingInfluenceMap BuildingInfluence;
		public static UnitInfluenceMap UnitInfluence;

		static ISoundEngine soundEngine;

		public static string Replay;

		public static string NetworkHost;
		public static int NetworkPort;

		public static bool skipMakeAnims = true;

		public static void Initialize(string mapName, Renderer renderer, int2 clientSize, int localPlayer, bool useAftermath)
		{
			Rules.LoadRules(mapName, useAftermath);

			for (int i = 0; i < 8; i++)
				players.Add(i, 
					new Player(i, i, 
						string.Format("Multi{0}", i),
						Race.Allies));

			localPlayerIndex = localPlayer;

			Rules.Map.InitOreDensity();

			controller = new Controller();
			worldRenderer = new WorldRenderer( renderer );

			SequenceProvider.Initialize(useAftermath);
			viewport = new Viewport( clientSize, Rules.Map.Offset, Rules.Map.Offset + Rules.Map.Size, renderer );

			world = new World();

			BuildingInfluence = new BuildingInfluenceMap();
			UnitInfluence = new UnitInfluenceMap();

			foreach (TreeReference treeReference in Rules.Map.Trees)
				world.Add(new Actor(treeReference.Image, 
					new int2(treeReference.Location),
					null));

			LoadMapBuildings(Rules.AllRules);
			LoadMapUnits(Rules.AllRules);

			PathFinder = new PathFinder(Rules.Map);

			soundEngine = new ISoundEngine();
			sounds = new Cache<string, ISoundSource>(LoadSound);

			if (Replay != "")
				orderManager = new OrderManager(new OrderSource[] { new ReplayOrderSource(Replay) });
			else
			{
				var orderSources = (string.IsNullOrEmpty(NetworkHost))
					? new OrderSource[] { new LocalOrderSource() }
					: new OrderSource[] { new LocalOrderSource(), new NetworkOrderSource(new TcpClient(NetworkHost, NetworkPort)) };
				orderManager = new OrderManager(orderSources, "replay.rep");
			}

			PlaySound("intro.aud", false);

			skipMakeAnims = false;
			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;

			chrome = new Chrome(renderer);
		}

		static void LoadMapBuildings(IniFile mapfile)
		{
			foreach (var s in mapfile.GetSection("STRUCTURES", true))
			{
				//num=owner,type,health,location,facing,trigger,unknown,shouldRepair
				var parts = s.Value.ToLowerInvariant().Split(',');
				var loc = int.Parse(parts[3]);
				world.Add(new Actor(parts[1], new int2(loc % 128, loc / 128), players[0]));
			}
		}

		static void LoadMapUnits(IniFile mapfile)
		{
			foreach (var s in mapfile.GetSection("UNITS", true))
			{
				//num=owner,type,health,location,facing,action,trigger
				var parts = s.Value.Split(',');
				var loc = int.Parse(parts[3]);
				world.Add(new Actor(parts[1].ToLowerInvariant(), new int2(loc % 128, loc / 128),
					players.Values.FirstOrDefault(p => p.PlayerName == parts[0]) 
					?? players[0]));
			}
		}

		static Cache<string, ISoundSource> sounds;

		static ISoundSource LoadSound(string filename)
		{
			var data = AudLoader.LoadSound(FileSystem.Open(filename));
			return soundEngine.AddSoundSourceFromPCMData(data, filename,
				new AudioFormat()
				{
					ChannelCount = 1,
					FrameCount = data.Length / 2,
					Format = SampleFormat.Signed16Bit,
					SampleRate = 22050
				});
		}

		public static void PlaySound(string name, bool loop)
		{
			var sound = sounds[name];
			// todo: positioning
			soundEngine.Play2D(sound, loop, false, false);
		}

		static int lastTime = Environment.TickCount;
		public static int timestep = 40;

		public static void ResetTimer()
		{
			lastTime = Environment.TickCount;
		}

		const int oreFrequency = 30;
		static int oreTicks = oreFrequency;
		public static int RenderFrame = 0;

		public static Chat chat = new Chat();

		public static void Tick()
		{
			int t = Environment.TickCount;
			int dt = t - lastTime;
			if (dt >= timestep)
			{
				using (new PerfSample("tick_time"))
				{
					lastTime += timestep;

					orderManager.TickImmediate();

					if (orderManager.IsReadyForNextFrame)
					{
						orderManager.Tick();
						if (controller.orderGenerator != null)
							controller.orderGenerator.Tick();

						if (--oreTicks == 0)
						{
							using (new PerfSample("ore"))
								Rules.Map.GrowOre(SharedRandom);
							oreTicks = oreFrequency;
						}

						world.Tick();
						UnitInfluence.Tick();
						foreach (var player in players.Values)
							player.Tick();
					}
					else
						if (orderManager.FrameNumber == 0)
							lastTime = Environment.TickCount;
				}

				PerfHistory.Tick();
			}

			using (new PerfSample("render"))
			{
				++RenderFrame;
				viewport.cursor = controller.ChooseCursor();
				viewport.DrawRegions();
			}

			PerfHistory.items["render"].Tick();
			PerfHistory.items["batches"].Tick();
		}

		public static bool IsCellBuildable(int2 a, UnitMovementType umt)
		{
			return IsCellBuildable(a, umt, null);
		}

		public static bool IsCellBuildable(int2 a, UnitMovementType umt, Actor toIgnore)
		{
			if (BuildingInfluence.GetBuildingAt(a) != null) return false;
			if (UnitInfluence.GetUnitAt(a) != null && UnitInfluence.GetUnitAt(a) != toIgnore) return false;

			return Rules.Map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(umt,
					Rules.TileSet.GetWalkability(Rules.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}

		public static bool IsWater(int2 a)
		{
			return Rules.Map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(UnitMovementType.Float,
					Rules.TileSet.GetWalkability(Rules.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}

		static IEnumerable<Actor> FindUnits(float2 a, float2 b)
		{
			var min = float2.Min(a, b);
			var max = float2.Max(a, b);

			var rect = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);

			return world.Actors
				.Where(x => x.Bounds.IntersectsWith(rect));
		}

		public static IEnumerable<Actor> FindUnitsInCircle(float2 a, float r)
		{
			return FindUnits(a - new float2(r, r), a + new float2(r, r))
				.Where(x => (x.CenterLocation - a).LengthSquared < r * r);
		}

		public static IEnumerable<int2> FindTilesInCircle(int2 a, int r)
		{
			var min = a - new int2(r, r);
			var max = a + new int2(r, r);
			if (min.X < 0) min.X = 0;
			if (min.Y < 0) min.Y = 0;
			if (max.X > 127) max.X = 127;
			if (max.Y > 127) max.Y = 127;

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (r * r >= (new int2(i, j) - a).LengthSquared)
						yield return new int2(i, j);
		}

		public static IEnumerable<Actor> SelectActorsInBox(float2 a, float2 b)
		{
			return FindUnits(a, b)
				.Where( x => x.unitInfo.Selectable )
				.GroupBy(x => (x.Owner == LocalPlayer) ? x.unitInfo.SelectionPriority : 0)
				.OrderByDescending(g => g.Key)
				.Select( g => g.AsEnumerable() )
				.DefaultIfEmpty( new Actor[] {} )
				.FirstOrDefault();
		}

		public static int GetDistanceToBase(int2 b, Player p)
		{
			var building = BuildingInfluence.GetNearestBuilding(b);
			if (building == null || building.Owner != p)
				return int.MaxValue;

			return BuildingInfluence.GetDistanceToBuilding(b);
		}

		public static Random SharedRandom = new Random(0);		/* for things that require sync */
		public static Random CosmeticRandom = new Random();		/* for things that are just fluff */

		public static readonly Pair<VoicePool, VoicePool> SovietVoices =
			Pair.New(
				new VoicePool("ackno", "affirm1", "noprob", "overout", "ritaway", "roger", "ugotit"),
				new VoicePool("await1", "ready", "report1", "yessir1"));

		public static int2? FindAdjacentTile(Actor a, UnitMovementType umt)
		{
			var tiles = Footprint.Tiles(a, a.traits.Get<Traits.Building>());
			var min = tiles.Aggregate(int2.Min) - new int2(1, 1);
			var max = tiles.Aggregate(int2.Max) + new int2(1, 1);

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (IsCellBuildable(new int2(i, j), umt))
						return new int2(i, j);

			return null;
		}

		public static bool CanPlaceBuilding(BuildingInfo building, int2 xy, Actor toIgnore, bool adjust)
		{
			return !Footprint.Tiles(building, xy, adjust).Any(
				t => !Rules.Map.IsInMap(t.X, t.Y) || Rules.Map.ContainsResource(t) || !Game.IsCellBuildable(t,
					building.WaterBound ? UnitMovementType.Float : UnitMovementType.Wheel,
					toIgnore));
		}

		public static bool CanPlaceBuilding(BuildingInfo building, int2 xy, bool adjust)
		{
			return CanPlaceBuilding(building, xy, null, adjust);
		}

		public static bool IsCloseEnoughToBase(Player p, BuildingInfo bi, int2 position)
		{
			var maxDistance = bi.Adjacent + 2;	/* real-ra is weird. this is 1 GAP. */

			var search = new PathSearch()
			{
				heuristic = loc =>
				{
					var b = Game.BuildingInfluence.GetBuildingAt(loc);
					if (b != null && b.Owner == p) return 0;
					if ((loc - position).Length > maxDistance)
						return float.PositiveInfinity;	/* not quite right */
					return 1;
				},
				checkForBlocked = false,
				ignoreTerrain = true,
			};

			foreach (var t in Footprint.Tiles(bi, position)) search.AddInitialCell(t);

			return Game.PathFinder.FindPath(search).Count != 0;
		}

		public static void BuildUnit(Player player, string name)
		{
			var newUnitType = Rules.UnitInfo[ name ];
			var producerTypes = Rules.TechTree.UnitBuiltAt( newUnitType );
			// TODO: choose producer based on "primary building"
			var producer = world.Actors
				.Where( x => producerTypes.Contains( x.unitInfo ) && x.Owner == player )
				.FirstOrDefault();

			if (producer == null)
			{
			    player.CancelProduction(Rules.UnitCategory[name]);
			    return;
			}

			if( producer.traits.WithInterface<IProducer>().Any( p => p.Produce( producer, newUnitType ) ) )
				player.FinishProduction(Rules.UnitCategory[name]);
		}
	}
}
