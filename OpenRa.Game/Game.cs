using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Orders;
using OpenRa.Game.Support;
using OpenRa.Game.Traits;
using IjwFramework.Types;

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
		public static UserSettings Settings;
		
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

		public static bool skipMakeAnims = true;

		static Renderer renderer;
		static bool usingAftermath;
		static int2 clientSize;
		static HardwarePalette palette;
		static string mapName;
		public static Minimap minimap;
		public static Session LobbyInfo = new Session();
		public static int2[] SpawnPoints;
		static bool changePending;

		public static void ChangeMap(string mapName)
		{
			chat.AddLine(Color.White, "Debug", "Map change {0} -> {1}".F(Game.mapName, mapName));
			Game.changePending = false;
			Game.mapName = mapName;
			SheetBuilder.Initialize(renderer);
			SpriteSheetBuilder.Initialize();
			FileSystem.UnmountTemporaryPackages();
			Rules.LoadRules(mapName, usingAftermath);
			palette = new HardwarePalette(renderer, Rules.Map);

			world = new World();
			Game.world.ActorAdded += a => 
			{ 
				if (a.Owner != null && a.Info.Traits.Contains<OwnedActorInfo>()) 
					a.Owner.Shroud.Explore(a); 
			};

			var worldActor = new Actor("World", new int2(int.MaxValue, int.MaxValue), null);
			Game.world.Add(worldActor);

			for (int i = 0; i < 8; i++)
			{
				var race = players.ContainsKey(i) ? players[i].Race : Race.Allies;
				var name = players.ContainsKey(i) ? players[i].PlayerName : "Player {0}".F(i+1);
				players[i] = new Player(i, LobbyInfo.Clients.FirstOrDefault(a => a.Index == i));
			}

			Rules.Map.InitOreDensity();
			worldRenderer = new WorldRenderer(renderer);

			SequenceProvider.Initialize(usingAftermath);
			viewport = new Viewport(clientSize, Rules.Map.Offset, Rules.Map.Offset + Rules.Map.Size, renderer);

			minimap = new Minimap(renderer);

			BuildingInfluence = new BuildingInfluenceMap();
			UnitInfluence = new UnitInfluenceMap();

			skipMakeAnims = true;
			foreach (var treeReference in Rules.Map.Trees)
				world.Add(new Actor(treeReference.Image, new int2(treeReference.Location), null));
			
			LoadMapActors(Rules.AllRules);
			skipMakeAnims = false;

			PathFinder = new PathFinder();

			chrome = new Chrome(renderer);

			oreFrequency = (int)(Rules.General.GrowthRate * 60 * 25);
			oreTicks = oreFrequency;

			SpawnPoints = Rules.AllRules.GetSection("Waypoints")
				.Select(kv => Pair.New(int.Parse(kv.Key), new int2(int.Parse(kv.Value) % 128, int.Parse(kv.Value) / 128)))
				.Where(a => a.First < 8)
				.Select(a => a.Second)
				.ToArray();
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

			if (Settings.Replay != "")
				orderManager = new OrderManager(new IOrderSource[] { new ReplayOrderSource(Settings.Replay) });
			else
			{
				var orderSources = (string.IsNullOrEmpty(Settings.NetworkHost))
					? new IOrderSource[] { new LocalOrderSource() }
					: new IOrderSource[] { new LocalOrderSource(), new NetworkOrderSource(new TcpClient(Settings.NetworkHost, Settings.NetworkPort)) };
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
				world.Add(new Actor(parts[1].ToLowerInvariant(), new int2(loc % 128, loc / 128),
					players.Values.FirstOrDefault(p => p.InternalName == parts[0]) ?? players[0]));
			}
		}

		static int lastTime = Environment.TickCount;

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
			if (changePending && PackageDownloader.IsIdle())
			{
				ChangeMap(LobbyInfo.GlobalSettings.Map);
				return;
			}

			int t = Environment.TickCount;
			int dt = t - lastTime;
			if (dt >= Settings.Timestep)
			{
				using (new PerfSample("tick_time"))
				{
					lastTime += Settings.Timestep;
					UpdatePalette(world.Actors.SelectMany(
						a => a.traits.WithInterface<IPaletteModifier>()));
					minimap.Update();
					chrome.Tick();

					orderManager.TickImmediate();

					if (orderManager.IsReadyForNextFrame)
					{
						orderManager.Tick();
						if (controller.orderGenerator != null)
							controller.orderGenerator.Tick();

						if (--oreTicks == 0)
							using (new PerfSample("ore"))
							{
								Rules.Map.GrowOre(SharedRandom);
								minimap.InvalidateOre();
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
			return IsActorCrushableByMovementType(a, b.traits.GetOrDefault<IMovement>().GetMovementType());
		}
		
		public static bool IsActorPathableToCrush(Actor a, UnitMovementType umt)
		{
			return a != null &&
					a.traits.WithInterface<ICrushable>()
					.Any(c => c.IsPathableCrush(umt, a.Owner));
		}
		
		public static bool IsActorCrushableByMovementType(Actor a, UnitMovementType umt)
		{
			return a != null &&
				a.traits.WithInterface<ICrushable>()
				.Any(c => c.IsCrushableBy(umt, a.Owner));
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
				.Where(x => x.GetBounds(true).IntersectsWith(rect));
		}

		public static IEnumerable<Actor> FindUnitsInCircle(float2 a, float r)
		{
			var min = a - new float2(r, r);
			var max = a + new float2(r, r);

			var rect = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);

			var inBox = world.Actors.Where(x => x.GetBounds(false).IntersectsWith(rect));

			return inBox.Where(x => (x.CenterLocation - a).LengthSquared < r * r);
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
				.Where( x => x.traits.Contains<Selectable>() )
				.GroupBy(x => (x.Owner == LocalPlayer) ? x.Info.Traits.Get<SelectableInfo>().Priority : 0)
				.OrderByDescending(g => g.Key)
				.Select( g => g.AsEnumerable() )
				.DefaultIfEmpty( new Actor[] {} )
				.FirstOrDefault();
		}

		public static Random SharedRandom = new Random(0);		/* for things that require sync */
		public static Random CosmeticRandom = new Random();		/* for things that are just fluff */

		public static bool CanPlaceBuilding(string name, BuildingInfo building, int2 xy, Actor toIgnore, bool adjust)
		{
			return !Footprint.Tiles(name, building, xy, adjust).Any(
				t => !Rules.Map.IsInMap(t.X, t.Y) || Rules.Map.ContainsResource(t) || !Game.IsCellBuildable(t,
					building.WaterBound ? UnitMovementType.Float : UnitMovementType.Wheel,
					toIgnore));
		}

		public static bool IsCloseEnoughToBase(Player p, string buildingName, BuildingInfo bi, int2 position)
		{
			var maxDistance = bi.Adjacent + 1;

			var search = new PathSearch()
			{
				heuristic = loc =>
				{
					var b = Game.BuildingInfluence.GetBuildingAt(loc);
					if (b != null && b.Owner == p && b.Info.Traits.Get<BuildingInfo>().BaseNormal) return 0;
					if ((loc - position).Length > maxDistance)
						return float.PositiveInfinity;	/* not quite right */
					return 1;
				},
				checkForBlocked = false,
				ignoreTerrain = true,
			};

			foreach (var t in Footprint.Tiles(buildingName, bi, position)) search.AddInitialCell(t);

			return Game.PathFinder.FindPath(search).Count != 0;
		}

		public static void SyncLobbyInfo(string data)
		{
			var session = new Session();
			var ys = MiniYaml.FromString(data);
			foreach (var y in ys)
			{
				if (y.Key == "GlobalSettings")
				{
					FieldLoader.Load(session.GlobalSettings, y.Value);
					continue;
				}

				int index;
				if (!int.TryParse(y.Key, out index))
					continue;	// not a player.

				var client = new Session.Client();
				FieldLoader.Load(client, y.Value);
				session.Clients.Add(client);

				players[index].SyncFromLobby(client);
			}

			LobbyInfo = session;

			if (Game.orderManager.FramesAhead != LobbyInfo.GlobalSettings.OrderLatency
				&& !Game.orderManager.GameStarted)
			{
				Game.orderManager.FramesAhead = LobbyInfo.GlobalSettings.OrderLatency;
				Game.chat.AddLine(Color.White, "Server",
					"Order lag is now {0} frames.".F(LobbyInfo.GlobalSettings.OrderLatency));
			}

			if (PackageDownloader.SetPackageList(LobbyInfo.GlobalSettings.Packages)
				|| mapName != LobbyInfo.GlobalSettings.Map)
				changePending = true;
		}

		public static void StartGame()
		{
			var available = SpawnPoints.ToList();
			var taken = new List<int2>();

			foreach (var client in LobbyInfo.Clients)
			{
				// todo: allow players to choose their own spawn points.
				// only select a point for them if they didn't.

				// todo: spawn more than one unit, in most cases!

				var sp = ChooseSpawnPoint(available, taken);
				world.Add(new Actor("mcv", sp, players[client.Index]));
			}

			Game.viewport.GoToStartLocation();
			orderManager.StartGame();
		}

		static int2 ChooseSpawnPoint(List<int2> available, List<int2> taken)
		{
			if (available.Count == 0)
				throw new InvalidOperationException("No free spawnpoint.");

			var n = taken.Count == 0 
				? Game.SharedRandom.Next(available.Count)
				: available			// pick the most distant spawnpoint from everyone else
					.Select((k,i) => Pair.New(k,i))
					.OrderByDescending(a => taken.Sum(t => (t - a.First).LengthSquared))
					.Select(a => a.Second)
					.First();
			
			var sp = available[n];
			available.RemoveAt(n);
			taken.Add(sp);
			return sp;
		}
	}
}
