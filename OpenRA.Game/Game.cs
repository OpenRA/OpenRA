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
		public static int CellSize { get { return modData.Manifest.TileSize; } }

		public static ModData modData;
		private static WorldRenderer worldRenderer;

		public static Viewport viewport;
		public static Settings Settings;

		internal static OrderManager orderManager;

		public static XRandom CosmeticRandom = new XRandom();	// not synced

		public static Renderer Renderer;
		public static bool HasInputFocus = false;
		
		public static void MoveViewport(float2 loc)
		{
			viewport.Center(loc);
		}

		internal static void JoinServer(string host, int port)
		{
			if (orderManager != null) orderManager.Dispose();

			var replayFilename = ChooseReplayFilename();
			string path = Path.Combine( Game.SupportDir, "Replays" );
			if( !Directory.Exists( path ) ) Directory.CreateDirectory( path );
			var replayFile = File.Create( Path.Combine( path, replayFilename ) );

			orderManager = new OrderManager( host, port, new ReplayRecorderConnection( new NetworkConnection( host, port ), replayFile ) );
			lastConnectionState = ConnectionState.PreConnecting;
			ConnectionStateChanged(orderManager);
		}

		static string ChooseReplayFilename()
		{
			return DateTime.UtcNow.ToString("OpenRA-yyyy-MM-ddThhmmssZ.rep");
		}

		static void JoinLocal()
		{
			if (orderManager != null) orderManager.Dispose();
			orderManager = new OrderManager("<no server>", -1, new EchoConnection());
			lastConnectionState = ConnectionState.PreConnecting;
			ConnectionStateChanged( orderManager );
		}

		internal static int RenderFrame = 0;
		internal static int LocalTick { get { return orderManager.LocalFrameNumber; } }
		const int NetTickScale = 3;		// 120ms net tick for 40ms local tick

		internal static event Action<OrderManager> ConnectionStateChanged = _ => { };
		static ConnectionState lastConnectionState = ConnectionState.PreConnecting;
		public static int LocalClientId { get { return orderManager.Connection.LocalClientId; } }

		static void Tick( OrderManager orderManager, Viewport viewPort )
		{
			if (orderManager.Connection.ConnectionState != lastConnectionState)
			{
				lastConnectionState = orderManager.Connection.ConnectionState;
				ConnectionStateChanged( orderManager );
			}

			Tick( orderManager );
			if( orderManager.world != worldRenderer.world )
				Tick( worldRenderer.world.orderManager );

			using (new PerfSample("render"))
			{
				++RenderFrame;
				viewport.DrawRegions(worldRenderer);
				Sound.SetListenerPosition(viewport.Location + .5f * new float2(viewport.Width, viewport.Height));
			}

			PerfHistory.items["render"].Tick();
			PerfHistory.items["batches"].Tick();
			PerfHistory.items["text"].Tick();
			PerfHistory.items["cursor"].Tick();

			MasterServerQuery.Tick();
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

					var isNetTick = LocalTick % NetTickScale == 0;

					if( !isNetTick || orderManager.IsReadyForNextFrame )
					{
						++orderManager.LocalFrameNumber;

						Log.Write( "debug", "--Tick: {0} ({1})", LocalTick, isNetTick ? "net" : "local" );

						if( isNetTick ) orderManager.Tick();

						Sync.CheckSyncUnchanged(world, () =>
							{
								world.OrderGenerator.Tick(world);
								world.Selection.Tick(world);
							});
						
						world.Tick();
						worldRenderer.Tick();

						PerfHistory.Tick();
					}
					else
						if( orderManager.NetFrameNumber == 0 )
							orderManager.LastTickTime = Environment.TickCount;
				}
		}

		internal static event Action LobbyInfoChanged = () => { };

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
			viewport = new Viewport(new int2(Renderer.Resolution), map.TopLeft, map.BottomRight, Renderer);
			orderManager.world = new World(modData.Manifest, map, orderManager);
			worldRenderer = new WorldRenderer(orderManager.world);

			if (orderManager.GameStarted) return;
			Widget.SelectedWidget = null;

			orderManager.LocalFrameNumber = 0;

			orderManager.StartGame();
			worldRenderer.RefreshPalette();
			AfterGameStart( orderManager.world );
		}

		public static void DispatchMouseInput(MouseInputEvent ev, MouseEventArgs e, Modifiers modifierKeys)
		{
			Sync.CheckSyncUnchanged( orderManager.world, () =>
			{
				var mi = new MouseInput
				{
					Button = (MouseButton)(int)e.Button,
					Event = ev,
					Location = new int2( e.Location ),
					Modifiers = modifierKeys,
				};
				Widget.HandleInput( mi );
			} );
		}

		public static bool IsHost
		{
			get { return orderManager.Connection.LocalClientId == 0; }
		}

		public static void HandleKeyEvent(KeyInput e)
		{
			Sync.CheckSyncUnchanged( orderManager.world, () =>
			{
				Widget.HandleKeyPress( e );
			} );
		}

		static Modifiers modifiers;
		public static Modifiers GetModifierKeys() { return modifiers; }
		public static void HandleModifierKeys(Modifiers mods) {	modifiers = mods; }

		internal static void Initialize(Arguments args)
		{
			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;

			var defaultSupport = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
												+ Path.DirectorySeparatorChar + "OpenRA";

			SupportDir = args.GetValue("SupportDir", defaultSupport);
			Settings = new Settings(SupportDir + "settings.yaml", args);

			// force master server upgrade -- remove once everyone is switched over.
			if (Settings.Server.MasterServer == "http://open-ra.org/master/")
				Settings.Server.MasterServer = "http://master.open-ra.org/";

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
			
			// Discard any invalid mods
			var mods = Settings.Game.Mods.Where( m => Mod.AllMods.ContainsKey( m ) ).ToArray();
			Console.WriteLine("Loading mods: {0}",string.Join(",",mods));
			
			modData = new ModData( mods );
			
			Sound.Initialize();
			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;
			PerfHistory.items["text"].hasNormalTick = false;
			PerfHistory.items["cursor"].hasNormalTick = false;

			
			JoinLocal();
			StartGame(modData.Manifest.ShellmapUid);

			Game.ConnectionStateChanged += orderManager =>
			{
				Widget.CloseWindow();
				switch( orderManager.Connection.ConnectionState )
				{
					case ConnectionState.PreConnecting:
						Widget.OpenWindow("MAINMENU_BG");
						break;
					case ConnectionState.Connecting:
						Widget.OpenWindow( "CONNECTING_BG",
							new Dictionary<string, object> { { "host", orderManager.Host }, { "port", orderManager.Port } } );
						break;
					case ConnectionState.NotConnected:
						Widget.OpenWindow( "CONNECTION_FAILED_BG",
							new Dictionary<string, object> { { "host", orderManager.Host }, { "port", orderManager.Port } } );
						break;
					case ConnectionState.Connected:
						var lobby = Widget.OpenWindow( "SERVER_LOBBY", new Dictionary<string, object> { { "orderManager", orderManager } } );
						lobby.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").ClearChat();
						lobby.GetWidget("CHANGEMAP_BUTTON").Visible = true;
						lobby.GetWidget("LOCKTEAMS_CHECKBOX").Visible = true;
						lobby.GetWidget("DISCONNECT_BUTTON").Visible = true;
						//r.GetWidget("INGAME_ROOT").GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").ClearChat();	
						break;
				}
			};

			modData.WidgetLoader.LoadWidget( new Dictionary<string,object>(), Widget.RootWidget, "PERF_BG" );
			Widget.OpenWindow("MAINMENU_BG");

			Game.orderManager.LastTickTime = Environment.TickCount;
		}

		static bool quit;
		internal static void Run()
		{
			while (!quit)
			{
				Tick( orderManager, viewport );
				Application.DoEvents();
			}
		}

		public static void Exit() { quit = true; }

		public static Action<Color,string,string> AddChatLine = (c,n,s) => {};

		public static void Debug(string s, params object[] args)
		{
			AddChatLine(Color.White, "Debug", String.Format(s,args)); 
		}

		public static void Disconnect()
		{
			orderManager.Dispose();
			var shellmap = modData.Manifest.ShellmapUid;
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
	}
}
