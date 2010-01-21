using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using IjwFramework.Types;
using OpenRa.FileFormats;
using OpenRa.GameRules;
using OpenRa.Graphics;
using OpenRa.Orders;
using OpenRa.Support;
using OpenRa.Traits;
using System.Windows.Forms;

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

		public static bool skipMakeAnims = true;

		internal static Renderer renderer;
		static int2 clientSize;
		static string mapName;
		internal static Session LobbyInfo = new Session();
		static bool changePending;

		public static void ChangeMap(string mapName)
		{
			var manifest = new Manifest(LobbyInfo.GlobalSettings.Mods);

			chat.AddLine(Color.White, "Debug", "Map change {0} -> {1}".F(Game.mapName, mapName));
			Game.changePending = false;
			Game.mapName = mapName;
			SheetBuilder.Initialize(renderer);
			SpriteSheetBuilder.Initialize();
			FileSystem.UnmountTemporaryPackages();

			foreach (var pkg in manifest.Packages)
				FileSystem.MountTemporary(new Package(pkg));

			Rules.LoadRules(mapName, manifest);

			world = null;	// trying to access the old world will NRE, rather than silently doing it wrong.
			world = new World();
			Game.world.ActorAdded += a => 
			{ 
				if (a.Owner != null && a.Info.Traits.Contains<OwnedActorInfo>()) 
					a.Owner.Shroud.Explore(a); 
			};

			SequenceProvider.Initialize(manifest.Sequences);
			viewport = new Viewport(clientSize, Game.world.Map.Offset, Game.world.Map.Offset + Game.world.Map.Size, renderer);

			skipMakeAnims = true;
			foreach (var treeReference in Game.world.Map.Trees)
				world.CreateActor(treeReference.Image, new int2(treeReference.Location), null);
			
			world.LoadMapActors(Rules.AllRules);
			skipMakeAnims = false;

			chrome = new Chrome(renderer);
		}

		internal static void Initialize(string mapName, Renderer renderer, int2 clientSize, int localPlayer, Controller controller)
		{
			//localPlayerIndex = localPlayer;
			Game.renderer = renderer;
			Game.clientSize = clientSize;

			// todo
			Sound.Initialize();
			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;
			Game.controller = controller;

			ChromeProvider.Initialize("chrome.xml");

			ChangeMap(mapName);

			if (Settings.Replay != "")
				orderManager = new OrderManager(new IOrderSource[] { new ReplayOrderSource(Settings.Replay) });
			else
			{
				var orderSources = (string.IsNullOrEmpty(Settings.NetworkHost))
					? new IOrderSource[] { new LocalOrderSource() }
					: new IOrderSource[] { new LocalOrderSource(), new NetworkOrderSource(Settings.NetworkHost, Settings.NetworkPort) };
				orderManager = new OrderManager(orderSources, "replay.rep");
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

					orderManager.TickImmediate( world );

					if (orderManager.IsReadyForNextFrame)
					{
						orderManager.Tick( world );
						if (controller.orderGenerator != null)
							controller.orderGenerator.Tick( world );

						world.Tick();
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
				viewport.DrawRegions( world );
			}

			PerfHistory.items["render"].Tick();
			PerfHistory.items["batches"].Tick();
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

				world.players[index].SyncFromLobby(client);
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
			var available = world.Map.SpawnPoints.ToList();
			var taken = new List<int2>();

			foreach (var client in LobbyInfo.Clients)
			{
				// todo: allow players to choose their own spawn points.
				// only select a point for them if they didn't.

				// todo: spawn more than one unit, in most cases!

				var sp = ChooseSpawnPoint(available, taken);
				world.CreateActor("mcv", sp, world.players[client.Index]);
			}

			Game.viewport.GoToStartLocation( Game.world.LocalPlayer );
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

		internal static void DispatchMouseInput(MouseInputEvent ev, MouseEventArgs e, Keys ModifierKeys)
		{
			int sync = Game.world.SyncHash();

			Game.viewport.DispatchMouseInput( world, 
				new MouseInput
				{
					Button = (MouseButton)(int)e.Button,
					Event = ev,
					Location = new int2(e.Location),
					Modifiers = (Modifiers)(int)ModifierKeys,
				});

			if( sync != Game.world.SyncHash() )
				throw new InvalidOperationException( "Desync in DispatchMouseInput" );
		}

		internal static void HandleKeyDown( KeyEventArgs e )
		{
			int sync = Game.world.SyncHash();

			/* hack hack hack */
			if( e.KeyCode == Keys.F8 && !Game.orderManager.GameStarted )
			{
				Game.controller.AddOrder(
					new Order( "ToggleReady", Game.world.LocalPlayer.PlayerActor, null, int2.Zero, "" ) { IsImmediate = true } );
			}

			/* temporary hack: DO NOT LEAVE IN */
			if( e.KeyCode == Keys.F2 )
				Game.world.LocalPlayer = Game.world.players[ ( Game.world.LocalPlayer.Index + 1 ) % 4 ];
			if( e.KeyCode == Keys.F3 )
				Game.controller.orderGenerator = new SellOrderGenerator();
			if( e.KeyCode == Keys.F4 )
				Game.controller.orderGenerator = new RepairOrderGenerator();

			if( !Game.chat.isChatting )
				if( e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 )
					Game.controller.DoControlGroup( world, (int)e.KeyCode - (int)Keys.D0, (Modifiers)(int)e.Modifiers );

			if( sync != Game.world.SyncHash() )
				throw new InvalidOperationException( "Desync in OnKeyDown" );
		}

		internal static void HandleKeyPress( KeyPressEventArgs e )
		{
			int sync = Game.world.SyncHash();

			if( e.KeyChar == '\r' )
				Game.chat.Toggle();
			else if( Game.chat.isChatting )
				Game.chat.TypeChar( e.KeyChar );

			if( sync != Game.world.SyncHash() )
				throw new InvalidOperationException( "Desync in OnKeyPress" );
		}
	}
}
