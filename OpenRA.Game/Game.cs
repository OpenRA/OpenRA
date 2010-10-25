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

		public static event Action<OrderManager> ConnectionStateChanged = _ => { };
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
				viewport.DrawRegions(worldRenderer, new DefaultInputHandler( orderManager.world ));
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

						PerfHistory.Tick();
					}
					else
						if( orderManager.NetFrameNumber == 0 )
							orderManager.LastTickTime = Environment.TickCount;
				}
		}

		public static event Action LobbyInfoChanged = () => { };
		public static event Action ConnectedToLobby = () => { };

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

		public static bool IsHost
		{
			get { return orderManager.Connection.LocalClientId == 0; }
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
			
			// Discard any invalid mods
			var mods = Settings.Game.Mods.Where( m => Mod.AllMods.ContainsKey( m ) ).ToArray();
			Console.WriteLine("Loading mods: {0}",string.Join(",",mods));
			
			modData = new ModData( mods );

			// when this client is running in dedicated mode ...
			if (Settings.Server.IsDedicated)
			{
				// it may specify a yaml extension file (to add non synced traits)
				if (!string.IsNullOrEmpty(Settings.Server.ExtensionYaml))
				{
					var r = modData.Manifest.LocalRules.ToList();
					r.Add(Settings.Server.ExtensionYaml);
					modData.Manifest.LocalRules = r.ToArray();
				} 
				// and a dll to the assemblies (to add those non synced traits)
				if (!string.IsNullOrEmpty(Settings.Server.ExtensionDll))
				{
					var r = modData.Manifest.LocalAssemblies.ToList();
					r.Add(Settings.Server.ExtensionDll);
					modData.Manifest.LocalAssemblies = r.ToArray();
				}

				if (!string.IsNullOrEmpty(Settings.Server.ExtensionClass))
					Settings.Server.Extension = modData.ObjectCreator.CreateObject<IServerExtension>(Settings.Server.ExtensionClass);
			}

			Sound.Initialize();
			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;
			PerfHistory.items["text"].hasNormalTick = false;
			PerfHistory.items["cursor"].hasNormalTick = false;


			if (!Settings.Graphics.UseNullRenderer)
			{
				JoinLocal();
				StartGame(modData.Manifest.ShellmapUid);

				Game.ConnectionStateChanged += om =>
               	{
               		Widget.CloseWindow();
               		switch (om.Connection.ConnectionState)
               		{
               			case ConnectionState.PreConnecting:
               				Widget.OpenWindow("MAINMENU_BG");
               				break;
               			case ConnectionState.Connecting:
               				Widget.OpenWindow("CONNECTING_BG",
               				                  new Dictionary<string, object>
               				                  	{{"host", om.Host}, {"port", om.Port}});
               				break;
               			case ConnectionState.NotConnected:
               				Widget.OpenWindow("CONNECTION_FAILED_BG",
               				                  new Dictionary<string, object>
               				                  	{{"host", om.Host}, {"port", om.Port}});
               				break;
               			case ConnectionState.Connected:
               				var lobby = Widget.OpenWindow("SERVER_LOBBY",
               				                              new Dictionary<string, object>
               				                              	{{"orderManager", om}});
               				lobby.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").ClearChat();
               				lobby.GetWidget("CHANGEMAP_BUTTON").Visible = true;
               				lobby.GetWidget("LOCKTEAMS_CHECKBOX").Visible = true;
               				lobby.GetWidget("DISCONNECT_BUTTON").Visible = true;

							// Inform whoever is willing to hear it that the player is connected to the lobby
               				if (ConnectedToLobby != null)
               					ConnectedToLobby();

							if (Settings.Server.IsDedicated)
							{
								// Force spectator as a default
								Game.orderManager.IssueOrder(Order.Command("spectator"));
							}

							if (Game.Settings.Server.Extension != null)
								Game.Settings.Server.Extension.OnLobbyUp(); 
							break;
               		}
               	};

				modData.WidgetLoader.LoadWidget(new Dictionary<string, object>(), Widget.RootWidget, "PERF_BG");
				Widget.OpenWindow("MAINMENU_BG");
			}else
			{
				JoinLocal();
				StartGame(modData.Manifest.ShellmapUid);

				Game.ConnectionStateChanged += om =>
				{
					Widget.CloseWindow();
					switch (om.Connection.ConnectionState)
					{
						case ConnectionState.PreConnecting:
							Widget.OpenWindow("MAINMENU_BG");
							break;
						case ConnectionState.Connecting:
							Widget.OpenWindow("CONNECTING_BG",
											  new Dictionary<string, object> { { "host", om.Host }, { "port", om.Port } });
							break;
						case ConnectionState.NotConnected:
							Widget.OpenWindow("CONNECTION_FAILED_BG",
											  new Dictionary<string, object> { { "host", om.Host }, { "port", om.Port } });
							break;
						case ConnectionState.Connected:
							var lobby = Widget.OpenWindow("SERVER_LOBBY",
														  new Dictionary<string, object> { { "orderManager", om } });                          

							// Inform whoever is willing to hear it that the player is connected to the lobby
							if (ConnectedToLobby != null)
								ConnectedToLobby();

							if (Settings.Server.IsDedicated)
							{
								// Force spectator as a default
								Game.orderManager.IssueOrder(Order.Command("spectator"));
							}

							if (Game.Settings.Server.Extension != null)
								Game.Settings.Server.Extension.OnLobbyUp(); 
							break;
					}
				};

				modData.WidgetLoader.LoadWidget(new Dictionary<string, object>(), Widget.RootWidget, "PERF_BG");
				Widget.OpenWindow("MAINMENU_BG");
			}

			if (Settings.Server.IsDedicated)
			{
				// Auto-host
				var map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Key;
				Server.Server.ServerMain(Game.modData, Settings, map);
				Game.JoinServer(IPAddress.Loopback.ToString(), Settings.Server.ListenPort);
			}

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
			if (IsHost)
				Server.Server.StopListening();

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

		public static void RejoinLobby(World world)
		{
			if (Game.IsHost && Game.Settings.Server.Extension != null)
				Game.Settings.Server.Extension.OnRejoinLobby(world);

			var map = orderManager.LobbyInfo.GlobalSettings.Map;
			var host = orderManager.Host;
			var port = orderManager.Port;
			var isHost = Game.IsHost;

			Disconnect();
			ConnectedToLobby += () =>
         	{
				if (world.LocalPlayer != null)
				{
					/* Try to get back the old slot */
					Game.orderManager.IssueOrder(Order.Command("race " + world.LocalPlayer.Country.Race));
					Game.orderManager.IssueOrder(Order.Command("slot " + world.LobbyInfo.ClientWithIndex(world.LocalPlayer.ClientIndex).Slot));
				}else /* a spectator */
				{
					Game.orderManager.IssueOrder(Order.Command("spectator"));
				}

         		ConnectedToLobby = null;
         	};
			if (isHost)
			{
				Server.Server.ServerMain(Game.modData, Settings, map);
				JoinServer(IPAddress.Loopback.ToString(), Settings.Server.ListenPort);
			}
			else
				JoinServer(host, port);
		}
	}
}
