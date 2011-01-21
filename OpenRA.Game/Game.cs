#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Support;
using OpenRA.Widgets;

using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA
{
	public static class Game
	{
		public static Utilities Utilities;
		
		public static int CellSize { get { return modData.Manifest.TileSize; } }

		public static ModData modData;
		private static WorldRenderer worldRenderer;

		public static Viewport viewport;
		public static Settings Settings;

		internal static OrderManager orderManager;
		static Server.Server server;

		public static XRandom CosmeticRandom = new XRandom();	// not synced

		public static Renderer Renderer;
		public static bool HasInputFocus = false;
		
		public static void MoveViewport(float2 loc)
		{
			viewport.Center(loc);
		}

		public static void JoinServer(string host, int port)
		{
			var replayFilename = ChooseReplayFilename();
			string path = Path.Combine( Game.SupportDir, "Replays" );
			if( !Directory.Exists( path ) ) Directory.CreateDirectory( path );
			var replayFile = File.Create( Path.Combine( path, replayFilename ) );

			JoinInner(new OrderManager(host, port, 
				new ReplayRecorderConnection(new NetworkConnection(host, port), replayFile)));
		}		

		static string ChooseReplayFilename()
		{
			return DateTime.UtcNow.ToString("OpenRA-yyyy-MM-ddTHHmmssZ.rep");
		}

		static void JoinInner(OrderManager om)
		{
			if (orderManager != null) orderManager.Dispose();
			orderManager = om;
			lastConnectionState = ConnectionState.PreConnecting;
			ConnectionStateChanged(orderManager);
		}

		public static void JoinReplay(string replayFile)
		{
			JoinInner(new OrderManager("<no server>", -1, new ReplayConnection(replayFile)));
		}

		static void JoinLocal()
		{
			JoinInner(new OrderManager("<no server>", -1, new EchoConnection()));
		}

		public static int RenderFrame = 0;
		public static int NetFrameNumber { get { return orderManager.NetFrameNumber; } }
		public static int LocalTick { get { return orderManager.LocalFrameNumber; } }
		const int NetTickScale = 3;		// 120ms net tick for 40ms local tick

		public static event Action<OrderManager> ConnectionStateChanged = _ => { };
		static ConnectionState lastConnectionState = ConnectionState.PreConnecting;
		public static int LocalClientId { get { return orderManager.Connection.LocalClientId; } }
		
		
		// Hacky workaround for orderManager visibility
		public static Widget OpenWindow(World world, string widget)
		{
			return Widget.OpenWindow(widget, new Dictionary<string,object>{{ "world", world }, { "orderManager", orderManager }, { "worldRenderer", worldRenderer }});
		}
		
		static object syncroot = new object();
		static Action tickActions = () => {};
		public static void RunAfterTick(Action a) { lock(syncroot) tickActions += a; }
		static void Tick( OrderManager orderManager, Viewport viewPort )
		{
			if (orderManager.Connection.ConnectionState != lastConnectionState)
			{
				lastConnectionState = orderManager.Connection.ConnectionState;
				ConnectionStateChanged( orderManager );
			}

			Tick( orderManager );
			if( worldRenderer != null && orderManager.world != worldRenderer.world )
				Tick( worldRenderer.world.orderManager );

			using (new PerfSample("render"))
			{
				++RenderFrame;
				viewport.DrawRegions(worldRenderer, new DefaultInputHandler( orderManager.world ));
				Sound.SetListenerPosition(viewport.Location + .5f * new float2(viewport.Width, viewport.Height));
			}

			PerfHistory.items["render"].Tick();
			PerfHistory.items["batches"].Tick();

			MasterServerQuery.Tick();
			Action a;
			lock(syncroot) { a = tickActions; tickActions = () => {}; }
			a();
		}

		private static void Tick( OrderManager orderManager )
		{
			int t = Environment.TickCount;
			int dt = t - orderManager.LastTickTime;
			if (dt >= Settings.Game.Timestep)
				using( new PerfSample( "tick_time" ) )
				{
					orderManager.LastTickTime += Settings.Game.Timestep;
					Widget.DoTick();
					var world = orderManager.world;
					if( orderManager.GameStarted && world.LocalPlayer != null )
						++Viewport.TicksSinceLastMove;
					Sound.Tick();
					Sync.CheckSyncUnchanged( world, () => { orderManager.TickImmediate(); } );

					if (world != null)
					{
						var isNetTick = LocalTick % NetTickScale == 0;

						if (!isNetTick || orderManager.IsReadyForNextFrame)
						{
							++orderManager.LocalFrameNumber;

							Log.Write("debug", "--Tick: {0} ({1})", LocalTick, isNetTick ? "net" : "local");

							if (isNetTick) orderManager.Tick();


							Sync.CheckSyncUnchanged(world, () =>
								{
									world.OrderGenerator.Tick(world);
									world.Selection.Tick(world);
								});

							world.Tick();

							PerfHistory.Tick();
						}
						else
							if (orderManager.NetFrameNumber == 0)
								orderManager.LastTickTime = Environment.TickCount;
					}
				}
		}

		public static event Action LobbyInfoChanged = () => { };

		internal static void SyncLobbyInfo()
		{
			LobbyInfoChanged();
		}

		public static event Action<World> AfterGameStart = _ => {};
		public static event Action BeforeGameStart = () => {};
		internal static void StartGame(string mapUID)
		{
			BeforeGameStart();

			var map = modData.PrepareMap(mapUID);
			viewport = new Viewport(new int2(Renderer.Resolution), map.Bounds, Renderer);
			orderManager.world = new World(modData.Manifest, map, orderManager);
			worldRenderer = new WorldRenderer(orderManager.world);

			if (orderManager.GameStarted) return;
			Widget.SelectedWidget = null;

			orderManager.LocalFrameNumber = 0;
			orderManager.StartGame();
			worldRenderer.RefreshPalette();
			AfterGameStart( orderManager.world );
		}

		public static bool IsHost
		{
			get { return orderManager.Connection.LocalClientId == 0; }
		}
		
		public static Dictionary<String, Mod> CurrentMods
		{
			get { return Mod.AllMods.Where( k => orderManager.LobbyInfo.GlobalSettings.Mods.Contains( k.Key )).ToDictionary( k => k.Key, k => k.Value ); }
		}

		static Modifiers modifiers;
		public static Modifiers GetModifierKeys() { return modifiers; }
		internal static void HandleModifierKeys(Modifiers mods) { modifiers = mods; }

		internal static void Initialize(Arguments args)
		{
			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;

			var defaultSupport = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
												+ Path.DirectorySeparatorChar + "OpenRA";

			SupportDir = args.GetValue("SupportDir", defaultSupport);
			FileSystem.SpecialPackageRoot = args.GetValue("SpecialPackageRoot", "");
			
			Utilities = new Utilities(args.GetValue("NativeUtilityPath", "."));
			
			Settings = new Settings(SupportDir + "settings.yaml", args);
			Settings.Save();

			Log.LogPath = SupportDir + "Logs" + Path.DirectorySeparatorChar;
			Log.AddChannel("perf", "perf.log");
			Log.AddChannel("debug", "debug.log");
			Log.AddChannel("sync", "syncreport.log");

			FileSystem.Mount("."); // Needed to access shaders
			Renderer.Initialize( Game.Settings.Graphics.Mode );
			Renderer.SheetSize = Settings.Game.SheetSize;
			Renderer = new Renderer();
			
			Console.WriteLine("Available mods:");
			foreach(var mod in Mod.AllMods)
				Console.WriteLine("\t{0}: {1} ({2})", mod.Key, mod.Value.Title, mod.Value.Version);
			
			InitializeWithMods(Settings.Game.Mods);
		}
		
		public static void InitializeWithMods(string[] mods)
		{
			// Clear static state if we have switched mods
			LobbyInfoChanged = () => {};
			AddChatLine = (a,b,c) => {};
			worldRenderer = null;
			if (server != null)
				server.Shutdown();
			if (orderManager != null)
				orderManager.Dispose();
			
			// Discard any invalid mods
			var mm = mods.Where( m => Mod.AllMods.ContainsKey( m ) ).ToArray();
			Console.WriteLine("Loading mods: {0}",string.Join(",",mm));

			
			modData = new ModData( mm );
			modData.LoadInitialAssets();
			
			Sound.Initialize();
			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;

			JoinLocal();
			viewport = new Viewport(new int2(Renderer.Resolution), Rectangle.Empty, Renderer);
			modData.WidgetLoader.LoadWidget( new Dictionary<string,object>(), Widget.RootWidget, "INIT_SETUP" );
		}
		
		public static void LoadShellMap()
		{
			StartGame(ChooseShellmap());
			Game.orderManager.LastTickTime = Environment.TickCount;
		}
		
        static string ChooseShellmap()
        {
            var shellmaps =  modData.AvailableMaps
                .Where(m => m.Value.UseAsShellmap);
			
			if (shellmaps.Count() == 0)
				throw new InvalidDataException("No valid shellmaps available");
			
			return shellmaps.Random(CosmeticRandom).Key;
        }

		static bool quit;
		public static event Action OnQuit = () => {};
		internal static void Run()
		{
			while (!quit)
			{
				Tick( orderManager, viewport );
				Application.DoEvents();
			}
			OnQuit();
		}

		public static void Exit() { quit = true; }

		public static Action<Color,string,string> AddChatLine = (c,n,s) => {};

		public static void Debug(string s, params object[] args)
		{
			AddChatLine(Color.White, "Debug", String.Format(s,args)); 
		}

		public static void Disconnect()
		{
			if (IsHost && server != null)
				server.Shutdown();

			orderManager.Dispose();
			var shellmap = ChooseShellmap();
			JoinLocal();
			StartGame(shellmap);

			Widget.CloseWindow();
			Widget.OpenWindow("MAINMENU_BG");
		}

		static string baseSupportDir = null;
		public static string SupportDir
		{
			set
			{
				var dir = value;

				// Expand paths relative to the personal directory
				if (dir.ElementAt(0) == '~')
					dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + dir.Substring(1);

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				baseSupportDir = dir + Path.DirectorySeparatorChar;
			}
			get { return baseSupportDir; }
		}

		public static T CreateObject<T>( string name )
		{
			return modData.ObjectCreator.CreateObject<T>( name );
		}

		public static void CreateAndJoinServer(Settings settings, string map)
		{
			server = new Server.Server(modData, settings, map);
			JoinServer(IPAddress.Loopback.ToString(), settings.Server.ListenPort);
		}
	}
}
