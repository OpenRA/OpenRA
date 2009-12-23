using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Support;
using OpenRa.Game.Traits;
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

		public static string Replay;

		public static string NetworkHost;
		public static int NetworkPort;

		public static bool skipMakeAnims = true;

		static Renderer renderer;
		static bool usingAftermath;
		static int2 clientSize;
		static HardwarePalette palette;

		public static void ChangeMap(string mapName)
		{
			SheetBuilder.Initialize(renderer);
			
			Rules.LoadRules(mapName, usingAftermath);
			palette = new HardwarePalette(renderer, Rules.Map);

			world = new World();

			for (int i = 0; i < 8; i++)
			{
				var race = players.ContainsKey(i) ? players[i].Race : Race.Allies;
				var name = players.ContainsKey(i) ? players[i].PlayerName : "Player {0}".F(i+1);

				var a = new Actor(null, new int2(int.MaxValue, int.MaxValue), null);
				players[i] = new Player(a, i, (PaletteType) i, name, race, "Multi{0}".F(i));
				a.Owner = players[i];
				a.traits.Add(new Traits.ProductionQueue(a));
				Game.world.Add(a);
			}

			var worldActor = new Actor(null, new int2(int.MaxValue, int.MaxValue), null);
			worldActor.traits.Add(new Traits.WaterPaletteRotation(worldActor));
			Game.world.Add(worldActor);

			Rules.Map.InitOreDensity();
			worldRenderer = new WorldRenderer(renderer);

			SequenceProvider.Initialize(usingAftermath);
			viewport = new Viewport(clientSize, Rules.Map.Offset, Rules.Map.Offset + Rules.Map.Size, renderer);

			BuildingInfluence = new BuildingInfluenceMap();
			UnitInfluence = new UnitInfluenceMap();

			skipMakeAnims = true;
			foreach (var treeReference in Rules.Map.Trees)
				world.Add(new Actor(Rules.UnitInfo[treeReference.Image],
					new int2(treeReference.Location),
					null));
			
			LoadMapActors(Rules.AllRules);
			skipMakeAnims = false;

			PathFinder = new PathFinder();

			chrome = new Chrome(renderer);

			oreFrequency = (int)(Rules.General.GrowthRate * 60 * 1000);
			oreTicks = oreFrequency;
		}

		public static void Initialize(string mapName, Renderer renderer, int2 clientSize, 
			int localPlayer, bool useAftermath, Controller controller)
		{
			localPlayerIndex = localPlayer;
			usingAftermath = useAftermath;
			Game.renderer = renderer;
			Game.clientSize = clientSize;

			// todo
			Sound.Initialize();
			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;
			Game.controller = controller;

			ChangeMap(mapName);

			if (Replay != "")
				orderManager = new OrderManager(new IOrderSource[] { new ReplayOrderSource(Replay) });
			else
			{
				var orderSources = (string.IsNullOrEmpty(NetworkHost))
					? new IOrderSource[] { new LocalOrderSource() }
					: new IOrderSource[] { new LocalOrderSource(), new NetworkOrderSource(new TcpClient(NetworkHost, NetworkPort)) };
				orderManager = new OrderManager(orderSources, "replay.rep");
			}
		}

		static void LoadMapActors(IniFile mapfile)
		{
			var toLoad = 
				mapfile.GetSection("STRUCTURES", true)
				.Concat(mapfile.GetSection("UNITS", true));

			foreach (var s in toLoad)
			{
				//num=owner,type,health,location,facing,...
				var parts = s.Value.Split( ',' );
				var loc = int.Parse(parts[3]);
				world.Add(new Actor(Rules.UnitInfo[parts[1].ToLowerInvariant()], new int2(loc % 128, loc / 128),
					players.Values.FirstOrDefault(p => p.InternalName == parts[0]) ?? players[0]));
			}
		}

		static int lastTime = Environment.TickCount;
		public static int timestep = 40;

		public static void ResetTimer()
		{
			lastTime = Environment.TickCount;
		}

		static int oreFrequency;
		static int oreTicks;
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
					UpdatePalette(world.Actors.SelectMany(
						a => a.traits.WithInterface<IPaletteModifier>()));
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
				viewport.DrawRegions();
			}

			PerfHistory.items["render"].Tick();
			PerfHistory.items["batches"].Tick();
		}

		static void UpdatePalette(IEnumerable<IPaletteModifier> paletteMods)
		{
			var b = new Bitmap(palette.Bitmap);
			foreach (var mod in paletteMods)
				mod.AdjustPalette(b);

			palette.Texture.SetData(b);
			renderer.PaletteTexture = palette.Texture;
		}

		public static bool IsCellBuildable(int2 a, UnitMovementType umt)
		{
			return IsCellBuildable(a, umt, null);
		}

		public static bool IsCellBuildable(int2 a, UnitMovementType umt, Actor toIgnore)
		{
			if (BuildingInfluence.GetBuildingAt(a) != null) return false;
			if (UnitInfluence.GetUnitsAt(a).Any(b => b != toIgnore)) return false;

			return Rules.Map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(umt,
					Rules.TileSet.GetWalkability(Rules.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}

		public static bool IsActorCrushableByActor(Actor a, Actor b)
		{
			return IsActorCrushableByMovementType(a, b.traits.WithInterface<IMovement>().FirstOrDefault().GetMovementType());
		}
		public static bool IsActorCrushableByMovementType(Actor a, UnitMovementType umt)
		{
			return a != null &&
				a.traits.WithInterface<ICrushable>()
				.Any(c => c.CrushableBy(umt) &&
					((c.IsCrushableByEnemy() && a.Owner != Game.LocalPlayer) ||
					(c.IsCrushableByFriend() && a.Owner == Game.LocalPlayer)));
		}
		
		public static bool IsWater(int2 a)
		{
			return Rules.Map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(UnitMovementType.Float,
					Rules.TileSet.GetWalkability(Rules.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}

		public static IEnumerable<Actor> FindUnits(float2 a, float2 b)
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
				.Where( x => x.Info.Selectable )
				.GroupBy(x => (x.Owner == LocalPlayer) ? x.Info.SelectionPriority : 0)
				.OrderByDescending(g => g.Key)
				.Select( g => g.AsEnumerable() )
				.DefaultIfEmpty( new Actor[] {} )
				.FirstOrDefault();
		}

		public static Random SharedRandom = new Random(0);		/* for things that require sync */
		public static Random CosmeticRandom = new Random();		/* for things that are just fluff */

		public static bool CanPlaceBuilding(BuildingInfo building, int2 xy, Actor toIgnore, bool adjust)
		{
			return !Footprint.Tiles(building, xy, adjust).Any(
				t => !Rules.Map.IsInMap(t.X, t.Y) || Rules.Map.ContainsResource(t) || !Game.IsCellBuildable(t,
					building.WaterBound ? UnitMovementType.Float : UnitMovementType.Wheel,
					toIgnore));
		}

		public static bool IsCloseEnoughToBase(Player p, BuildingInfo bi, int2 position)
		{
			var maxDistance = bi.Adjacent + 1;

			var search = new PathSearch()
			{
				heuristic = loc =>
				{
					var b = Game.BuildingInfluence.GetBuildingAt(loc);
					if (b != null && b.Owner == p && (b.Info as BuildingInfo).BaseNormal) return 0;
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
	}
}
