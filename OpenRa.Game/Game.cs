using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using IjwFramework.Types;
using OpenRa.FileFormats;
using OpenRa.GameRules;
using OpenRa.Graphics;
using OpenRa.Orders;
using OpenRa.Support;
using OpenRa.Traits;

namespace OpenRa
{
	public static class Game
	{
		public static readonly int CellSize = 24;

		public static World world;
		internal static Viewport viewport;
		public static Controller controller;
		internal static Chrome chrome;
		public static UserSettings Settings;
		
		internal static OrderManager orderManager;

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

		public static bool skipMakeAnims = true;

		internal static Renderer renderer;
		static bool usingAftermath;
		static int2 clientSize;
		static HardwarePalette palette;
		static string mapName;
		internal static Session LobbyInfo = new Session();
		internal static int2[] SpawnPoints;
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

			world = new World();
			Game.world.ActorAdded += a => 
			{ 
				if (a.Owner != null && a.Info.Traits.Contains<OwnedActorInfo>()) 
					a.Owner.Shroud.Explore(a); 
			};

			palette = new HardwarePalette(renderer, world.Map);

			for (int i = 0; i < 8; i++)
			{
				var race = players.ContainsKey(i) ? players[i].Race : Race.Allies;
				var name = players.ContainsKey(i) ? players[i].PlayerName : "Player {0}".F(i+1);
				players[i] = new Player(i, LobbyInfo.Clients.FirstOrDefault(a => a.Index == i));
			}

			SequenceProvider.Initialize(usingAftermath);
			viewport = new Viewport(clientSize, Game.world.Map.Offset, Game.world.Map.Offset + Game.world.Map.Size, renderer);

			skipMakeAnims = true;
			foreach (var treeReference in Game.world.Map.Trees)
				world.CreateActor(treeReference.Image, new int2(treeReference.Location), null);
			
			LoadMapActors(Rules.AllRules);
			skipMakeAnims = false;

			chrome = new Chrome(renderer);

			SpawnPoints = Rules.AllRules.GetSection("Waypoints")
				.Select(kv => Pair.New(int.Parse(kv.Key), new int2(int.Parse(kv.Value) % 128, int.Parse(kv.Value) / 128)))
				.Where(a => a.First < 8)
				.Select(a => a.Second)
				.ToArray();
		}

		internal static void Initialize(string mapName, Renderer renderer, int2 clientSize, 
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
				world.CreateActor(parts[1].ToLowerInvariant(), new int2(loc % 128, loc / 128),
					players.Values.FirstOrDefault(p => p.InternalName == parts[0]) ?? players[0]);
			}
		}

		static int lastTime = Environment.TickCount;

		public static void ResetTimer()
		{
			lastTime = Environment.TickCount;
		}

		public static int RenderFrame = 0;

		internal static Chat chat = new Chat();

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
					chrome.Tick();

					orderManager.TickImmediate();

					if (orderManager.IsReadyForNextFrame)
					{
						orderManager.Tick();
						if (controller.orderGenerator != null)
							controller.orderGenerator.Tick();

						world.Tick();
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
				UpdatePalette(world.Actors.SelectMany(
					a => a.traits.WithInterface<IPaletteModifier>()));
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

		public static Random SharedRandom = new Random(0);		/* for things that require sync */
		public static Random CosmeticRandom = new Random();		/* for things that are just fluff */

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
				world.CreateActor("mcv", sp, players[client.Index]);
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
