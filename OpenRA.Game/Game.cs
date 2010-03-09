#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Support;
using OpenRA.Traits;
using Timer = OpenRA.Support.Timer;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace OpenRA
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
		
		internal static Process Server;

		public static void LoadModPackages(Manifest manifest)
		{
			FileSystem.UnmountAll();
			Timer.Time("reset: {0}");

			foreach (var dir in manifest.Folders) FileSystem.Mount(dir);
			foreach (var pkg in manifest.Packages) FileSystem.Mount(pkg);
				
			Timer.Time("mount temporary packages: {0}");
		}
		
		public static void ChangeMap(string mapName)
		{
			Timer.Time( "----ChangeMap" );

			var manifest = new Manifest(LobbyInfo.GlobalSettings.Mods);
			Timer.Time( "manifest: {0}" );

			Game.changePending = false;
			Game.mapName = mapName;
			SheetBuilder.Initialize(renderer);
			
			LoadModPackages(manifest);
			
			Rules.LoadRules(mapName, manifest);
			Timer.Time( "load rules: {0}" );

			world = null;	// trying to access the old world will NRE, rather than silently doing it wrong.

			Player.ResetPlayerColorList();
			ChromeProvider.Initialize(manifest.Chrome);

			world = new World();
						
			Game.world.ActorAdded += a => 
			{ 
				if (a.Owner != null && a.Info.Traits.Contains<OwnedActorInfo>()) 
					a.Owner.Shroud.Explore(a); 
			};
			Timer.Time( "world: {0}" );
			
			SequenceProvider.Initialize(manifest.Sequences);
			viewport = new Viewport(clientSize, Game.world.Map.Offset, Game.world.Map.Offset + Game.world.Map.Size, renderer);
			Timer.Time( "ChromeProv, SeqProv, viewport: {0}" );

			skipMakeAnims = true;
			foreach (var actorReference in world.Map.Actors)
				world.CreateActor(actorReference.Name, actorReference.Location, world.players.Values.FirstOrDefault(p => p.InternalName == actorReference.Owner) ?? world.players[0]);	
			skipMakeAnims = false;
			Timer.Time( "map actors: {0}" );

			chrome = new Chrome(renderer);
			Timer.Time( "chrome: {0}" );

			Timer.Time( "----end ChangeMap" );
			chat.AddLine(Color.White, "Debug", "Map change {0} -> {1}".F(Game.mapName, mapName));
		}

		internal static void Initialize(string mapName, Renderer renderer, int2 clientSize, int localPlayer, Controller controller)
		{
			
			Game.renderer = renderer;
			Game.clientSize = clientSize;

			// todo
			Sound.Initialize();
			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;
			PerfHistory.items["text"].hasNormalTick = false;
			Game.controller = controller;
			
			ChangeMap(mapName);

			if (Settings.Replay != "")
				throw new NotImplementedException();
			else
			{
				JoinLocal();
			}
		}
		
		internal static void JoinServer(string host, int port)
		{
			orderManager = new OrderManager(new NetworkConnection( host, port ), "replay.rep");
		}
		
		internal static void JoinLocal()
		{
			orderManager = new OrderManager(new EchoConnection());
		}
		
		internal static void CreateServer()
		{
			Server = new Process();
			Server.StartInfo.FileName = "OpenRA.Server.exe";
            Server.StartInfo.Arguments = string.Join(" ",Game.LobbyInfo.GlobalSettings.Mods);
            Server.Start();
		}
		
		internal static void CloseServer()
		{
			Server.Kill();	
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
					chrome.Tick( world );

					orderManager.TickImmediate( world );

					if (orderManager.IsReadyForNextFrame)
					{
						orderManager.Tick(world);
						controller.orderGenerator.Tick(world);
						controller.selection.Tick(world);

						world.Tick();

						PerfHistory.Tick();
					}
					else
						if (orderManager.FrameNumber == 0)
							lastTime = Environment.TickCount;
				}
			}

			using (new PerfSample("render"))
			{
				++RenderFrame;
				viewport.DrawRegions( world );
			}

			PerfHistory.items["render"].Tick();
			PerfHistory.items["batches"].Tick();
			PerfHistory.items["text"].Tick();
		}

		public static void SyncLobbyInfo(string data)
		{
			var session = new Session();
			session.GlobalSettings.Mods = Settings.InitialMods;

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

			if (Game.orderManager.Connection.ConnectionState == ConnectionState.Connected)
				world.SetLocalPlayer(Game.orderManager.Connection.LocalClientId);

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

		public static void IssueOrder(Order o) { orderManager.IssueOrder(o); }	/* avoid exposing the OM to mod code */

		public static void StartGame()
		{
			Game.chat.Reset();
			
			var taken = LobbyInfo.Clients.Where(c => c.SpawnPoint != 0)
				.Select(c => world.Map.SpawnPoints.ElementAt(c.SpawnPoint - 1)).ToList();

			var available = world.Map.SpawnPoints.Except(taken).ToList();
				
			foreach (var client in LobbyInfo.Clients)
			{
				var sp = (client.SpawnPoint == 0) 
					? ChooseSpawnPoint(available, taken) 
					: world.Map.SpawnPoints.ElementAt(client.SpawnPoint - 1);

				foreach (var ssu in world.players[client.Index].PlayerActor
					.traits.WithInterface<IOnGameStart>())
					ssu.SpawnStartingUnits(world.players[client.Index], sp);
			}

			Game.viewport.GoToStartLocation( Game.world.LocalPlayer );
			orderManager.StartGame();
		}

		static int2 ChooseSpawnPoint(List<int2> available, List<int2> taken)
		{
			if (available.Count == 0)
				throw new InvalidOperationException("No free spawnpoint.");

			var n = taken.Count == 0 
				? world.SharedRandom.Next(available.Count)
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

		static int2 lastPos;
		public static void DispatchMouseInput(MouseInputEvent ev, MouseEventArgs e, Modifiers modifierKeys)
		{
			int sync = Game.world.SyncHash();

			if (ev == MouseInputEvent.Down)
				lastPos = new int2(e.Location);

			if (ev == MouseInputEvent.Move && (e.Button == MouseButtons.Middle || e.Button == (MouseButtons.Left | MouseButtons.Right)))
			{
				var p = new int2(e.Location);
				viewport.Scroll(lastPos - p);
				lastPos = p;
			}

			Game.viewport.DispatchMouseInput( world, 
				new MouseInput
				{
					Button = (MouseButton)(int)e.Button,
					Event = ev,
					Location = new int2(e.Location),
					Modifiers = modifierKeys,
				});

			if( sync != Game.world.SyncHash() )
				throw new InvalidOperationException( "Desync in DispatchMouseInput" );
		}

		public static void HandleKeyDown( KeyEventArgs e )
		{
			//int sync = Game.world.SyncHash();


			//if( sync != Game.world.SyncHash() )
			//    throw new InvalidOperationException( "Desync in OnKeyDown" );
		}

		public static bool IsHost
		{
			get { return orderManager.Connection.LocalClientId == 0; }
		}

		public static void HandleKeyPress( KeyPressEventArgs e, Modifiers modifiers )
		{
			int sync = Game.world.SyncHash();
			
			if( e.KeyChar == '\r' )
				Game.chat.Toggle();
			else if( Game.chat.isChatting )
				Game.chat.TypeChar( e.KeyChar );
			else
				if( e.KeyChar >= '0' && e.KeyChar <= '9' )
					Game.controller.selection.DoControlGroup( world, 
						e.KeyChar - '0', modifiers );

			if( sync != Game.world.SyncHash() )
				throw new InvalidOperationException( "Desync in OnKeyPress" );
		}

		public static void HandleModifierKeys(Modifiers mods)
		{
			controller.SetModifiers(mods);
		}

		static Size GetResolution(Settings settings)
		{
			var desktopResolution = Screen.PrimaryScreen.Bounds.Size;
			if (Game.Settings.Width > 0 && Game.Settings.Height > 0)
			{
				desktopResolution.Width = Game.Settings.Width;
				desktopResolution.Height = Game.Settings.Height;
			}
			return new Size(
				desktopResolution.Width,
				desktopResolution.Height);
		}

		public static void PreInit(Settings settings)
		{
			while (!Directory.Exists("mods"))
			{
				var current = Directory.GetCurrentDirectory();
				if (Directory.GetDirectoryRoot(current) == current)
					throw new InvalidOperationException("Unable to load MIX files.");
				Directory.SetCurrentDirectory("..");
			}

			
			LoadUserSettings(settings);
			Game.LobbyInfo.GlobalSettings.Mods = Game.Settings.InitialMods;
			
			// Load the default mod to access required files
			Game.LoadModPackages(new Manifest(Game.LobbyInfo.GlobalSettings.Mods));
			
			UiOverlay.ShowUnitDebug = Game.Settings.UnitDebug;
			WorldRenderer.ShowUnitPaths = Game.Settings.PathDebug;
			Renderer.SheetSize = Game.Settings.SheetSize;
			
			bool windowed = !Game.Settings.Fullscreen;
			var resolution = GetResolution(settings);
			renderer = new Renderer(resolution, windowed);
			resolution = renderer.Resolution;

			var controller = new Controller();	/* a bit of insane input routing */

			Game.Initialize(Game.Settings.Map, renderer, new int2(resolution), Game.Settings.Player, controller);

		//	ShowCursor(false);
			Game.ResetTimer();
		}

		static void LoadUserSettings(Settings settings)
		{
			Game.Settings = new UserSettings();
			var settingsFile = settings.GetValue("settings", "settings.ini");
			FileSystem.Mount("./");
			if (FileSystem.Exists(settingsFile))
				FieldLoader.Load(Game.Settings,
					new IniFile(FileSystem.Open(settingsFile)).GetSection("Settings"));
			FileSystem.UnmountAll();
		}

		static bool quit;
		internal static void Run()
		{
			while (!quit)
			{
				Game.Tick();
				Application.DoEvents();
			}
		}

		public static void Exit()
		{
			if (Server != null)
				CloseServer();
			
			quit = true;
		}
	}
}
