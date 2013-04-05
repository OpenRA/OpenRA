#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Support;
using OpenRA.Widgets;

using Mono.Nat;
using Mono.Nat.Pmp;
using Mono.Nat.Upnp;

using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA
{
	public static class Game
	{
		public static int CellSize { get { return modData.Manifest.TileSize; } }

		public static MouseButtonPreference mouseButtonPreference = new MouseButtonPreference();

		public static ModData modData;
		static WorldRenderer worldRenderer;

		public static INatDevice natDevice;

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
			JoinInner(new OrderManager(host, port,
				new ReplayRecorderConnection(new NetworkConnection(host, port), ChooseReplayFilename)));
		}

		static string ChooseReplayFilename()
		{
			return DateTime.UtcNow.ToString("OpenRA-yyyy-MM-ddTHHmmssZ");
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
			return Ui.OpenWindow(widget, new WidgetArgs() {{ "world", world }, { "orderManager", orderManager }, { "worldRenderer", worldRenderer }});
		}

		// Who came up with the great idea of making these things
		// impossible for the things that want them to access them directly?
		public static Widget OpenWindow(string widget, WidgetArgs args)
		{
			return Ui.OpenWindow(widget, new WidgetArgs(args)
			{
				{ "world", worldRenderer.world },
				{ "orderManager", orderManager },
				{ "worldRenderer", worldRenderer },
			});
		}

		// Load a widget with world, orderManager, worldRenderer args, without adding it to the widget tree
		public static Widget LoadWidget(World world, string id, Widget parent, WidgetArgs args)
		{
			return Game.modData.WidgetLoader.LoadWidget(new WidgetArgs(args)
			{
				{ "world", world },
				{ "orderManager", orderManager },
				{ "worldRenderer", worldRenderer },
			}, parent, id);
		}

		static ActionQueue delayedActions = new ActionQueue();
		public static void RunAfterTick(Action a) { delayedActions.Add(a); }
		public static void RunAfterDelay(int delay, Action a) { delayedActions.Add(a, delay); }

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
				Sound.SetListenerPosition(viewport.CenterLocation);
			}

			PerfHistory.items["render"].Tick();
			PerfHistory.items["batches"].Tick();
			PerfHistory.items["render_widgets"].Tick();
			PerfHistory.items["render_flip"].Tick();

			delayedActions.PerformActions();
		}

		static void Tick( OrderManager orderManager )
		{
			int t = Environment.TickCount;
			int dt = t - orderManager.LastTickTime;
			if (dt >= Settings.Game.Timestep)
				using( new PerfSample( "tick_time" ) )
				{
					orderManager.LastTickTime += Settings.Game.Timestep;
					Ui.Tick();
					var world = orderManager.world;
					if (orderManager.GameStarted)
						++Viewport.TicksSinceLastMove;
					Sound.Tick();
					Sync.CheckSyncUnchanged( world, () => { orderManager.TickImmediate(); } );

					if (world != null)
					{
						var isNetTick = LocalTick % NetTickScale == 0;

						if ((!isNetTick || orderManager.IsReadyForNextFrame) && !orderManager.GamePaused )
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
					
						viewport.Tick();
					}
				}
		}

		public static event Action LobbyInfoChanged = () => { };

		internal static void SyncLobbyInfo()
		{
			LobbyInfoChanged();
		}

		public static event Action BeforeGameStart = () => {};
		internal static void StartGame(string mapUID, bool isShellmap)
		{
			BeforeGameStart();

			var map = modData.PrepareMap(mapUID);
			viewport = new Viewport(new int2(Renderer.Resolution), map.Bounds, Renderer);
			orderManager.world = new World(modData.Manifest, map, orderManager, isShellmap);
			worldRenderer = new WorldRenderer(orderManager.world);

			if (orderManager.GameStarted) return;
			Ui.SelectedWidget = null;

			orderManager.LocalFrameNumber = 0;
			orderManager.LastTickTime = Environment.TickCount;
			orderManager.StartGame();
			worldRenderer.RefreshPalette();

			if (!isShellmap)
				Sound.PlayNotification(null, "Speech", "StartGame", null);
		}

		public static bool IsHost
		{
			get 
			{
				var client= orderManager.LobbyInfo.ClientWithIndex (
					orderManager.Connection.LocalClientId);
				return ((client!=null) && client.IsAdmin);
			}
		}

		public static Dictionary<String, Mod> CurrentMods
		{
			get { return Mod.AllMods.Where( k => modData.Manifest.Mods.Contains( k.Key )).ToDictionary( k => k.Key, k => k.Value ); }
		}

		static Modifiers modifiers;
		public static Modifiers GetModifierKeys() { return modifiers; }
		internal static void HandleModifierKeys(Modifiers mods) { modifiers = mods; }

		internal static void Initialize(Arguments args)
		{
			Console.WriteLine("Platform is {0}", Platform.CurrentPlatform);

			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;

			Settings = new Settings(Platform.SupportDir + "settings.yaml", args);

			Log.LogPath = Platform.SupportDir + "Logs" + Path.DirectorySeparatorChar;
			Log.AddChannel("perf", "perf.log");
			Log.AddChannel("debug", "debug.log");
			Log.AddChannel("sync", "syncreport.log");
			Log.AddChannel("server", "server.log");

			try {
				NatUtility.DeviceFound += DeviceFound;
				NatUtility.DeviceLost += DeviceLost;

				NatUtility.StartDiscovery();
				OpenRA.Log.Write("server", "NAT discovery started.");
			} catch (Exception e) {
				OpenRA.Log.Write("server", "Can't discover UPnP-enabled device: {0}", e);
				Settings.Server.AllowUPnP = false;
			}

			FileSystem.Mount("."); // Needed to access shaders
			Renderer.Initialize( Game.Settings.Graphics.Mode );
			Renderer = new Renderer();

			Console.WriteLine("Available mods:");
			foreach(var mod in Mod.AllMods)
				Console.WriteLine("\t{0}: {1} ({2})", mod.Key, mod.Value.Title, mod.Value.Version);

			Sound.Create(Settings.Sound.Engine);
			InitializeWithMods(Settings.Game.Mods);
		}

		public static void DeviceFound (object sender, DeviceEventArgs args)
		{
			natDevice = args.Device;

			Log.Write ("server", "NAT device discovered.");
			Log.Write ("server", "Type: {0}", natDevice.GetType ().Name);
			Log.Write ("server", "Your external IP is: {0}", natDevice.GetExternalIP ());

			foreach (Mapping mp in natDevice.GetAllMappings()) {
				Log.Write ("server", "Existing port mapping: protocol={0}, public={1}, private={2}", mp.Protocol, mp.PublicPort, mp.PrivatePort);
			}

			Settings.Server.AllowUPnP = true;
		}

		public static void DeviceLost (object sender, DeviceEventArgs args)
		{
			natDevice = args.Device;

			Log.Write("server", "NAT device Lost");
			Log.Write("server", "Type: {0}", natDevice.GetType().Name);

			Settings.Server.AllowUPnP = false;
		}

		public static void InitializeWithMods(string[] mods)
		{
			// Clear static state if we have switched mods
			LobbyInfoChanged = () => {};
			AddChatLine = (a,b,c) => {};
			ConnectionStateChanged = om => {};
			BeforeGameStart = () => {};
			Ui.ResetAll();

			worldRenderer = null;
			if (server != null)
				server.Shutdown();
			if (orderManager != null)
				orderManager.Dispose();

			// Discard any invalid mods, set RA as default
			var mm = mods.Where( m => Mod.AllMods.ContainsKey( m ) ).ToArray();
			if (mm.Length == 0) mm = new[] { "ra" };
			Console.WriteLine("Loading mods: {0}", mm.JoinWith(","));
			Settings.Game.Mods = mm;

			Sound.StopMusic();
			Sound.StopVideo();
			Sound.Initialize();

			modData = new ModData( mm );
			Renderer.InitializeFonts(modData.Manifest);
			modData.LoadInitialAssets();


			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;
			PerfHistory.items["render_widgets"].hasNormalTick = false;
			PerfHistory.items["render_flip"].hasNormalTick = false;

			JoinLocal();
			viewport = new Viewport(new int2(Renderer.Resolution), Rectangle.Empty, Renderer);

			if (Game.Settings.Server.Dedicated)
			{
				while (true)
				{
					Game.Settings.Server.Map = WidgetUtils.ChooseInitialMap(Game.Settings.Server.Map);
					Game.Settings.Save();
					Game.CreateServer(new ServerSettings(Game.Settings.Server));
					while(true)
					{
						System.Threading.Thread.Sleep(100);

						if((server.State == Server.ServerState.GameStarted)
						    && (server.conns.Count<=1))
						{
							Console.WriteLine("No one is playing, shutting down...");
							server.Shutdown();
							break;
						}
					}
					if (Game.Settings.Server.DedicatedLoop)
					{
						Console.WriteLine("Starting a new server instance...");
						continue;
					}
					else
						break;
				}
				System.Environment.Exit(0);
			}
			else
			{
				modData.LoadScreen.StartGame();
				Settings.Save();
			}
		}

		public static void LoadShellMap()
		{
			StartGame(ChooseShellmap(), true);
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
				var idealFrameTime = 1.0 / Settings.Graphics.MaxFramerate;
				var sw = new Stopwatch();

				Tick( orderManager, viewport );

				if (Settings.Graphics.CapFramerate)
				{
					var waitTime = idealFrameTime - sw.ElapsedTime();
					if (waitTime > 0)
						System.Threading.Thread.Sleep( TimeSpan.FromSeconds(waitTime) );
				}
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
			if (orderManager.world != null)
				orderManager.world.traitDict.PrintReport();

			orderManager.Dispose();
			CloseServer();
			JoinLocal();
		}

		public static void CloseServer()
		{
			if (server != null)
				server.Shutdown();
		}

		public static T CreateObject<T>( string name )
		{
			return modData.ObjectCreator.CreateObject<T>( name );
		}

		public static void CreateServer(ServerSettings settings)
		{
			server = new Server.Server(new IPEndPoint(IPAddress.Any, settings.ListenPort),
			                           Game.Settings.Game.Mods, settings, modData, natDevice);
		}

		public static int CreateLocalServer(string map)
		{
			var settings = new ServerSettings()
			{
				Name = "Skirmish Game",
				Map = map
			};

			// Work around a miscompile in mono 2.6.7:
			// booleans that default to true cannot be set false by an initializer
			settings.AdvertiseOnline = false;
			settings.AllowUPnP = false;

			server = new Server.Server(new IPEndPoint(IPAddress.Loopback, 0),
			                           Game.Settings.Game.Mods, settings, modData, natDevice);

			return server.Port;
		}

		public static bool IsCurrentWorld(World world)
		{
			return orderManager != null && orderManager.world == world;
		}

		public static void JoinExternalGame()
		{
			var addressParts = Game.Settings.Game.ConnectTo.Split(
				new [] { ':' }, StringSplitOptions.RemoveEmptyEntries);

			if (addressParts.Length < 1 || addressParts.Length > 2)
				return;

			var host = addressParts[0];
			var port = Exts.WithDefault(1234, () => int.Parse(addressParts[1]));

			Game.Settings.Game.ConnectTo = "";
			Game.Settings.Save();

			Game.JoinServer(host, port);
		}

		public static bool DownloadMap(string mapHash)
		{
			try
			{
				var mod = Game.CurrentMods.FirstOrDefault().Value.Id;
				var dirPath = "{1}maps{0}{2}".F(Path.DirectorySeparatorChar, Platform.SupportDir, mod);
				if(!Directory.Exists(dirPath))
					Directory.CreateDirectory(dirPath);
				var mapPath = "{1}{0}{2}".F(Path.DirectorySeparatorChar, dirPath, mapHash+".oramap");
				Console.Write("Trying to download map to {0} ... ".F(mapPath));
				WebClient webClient = new WebClient();
				webClient.DownloadFile(Game.Settings.Game.MapRepository + mapHash, mapPath);
				Game.modData.AvailableMaps.Add(mapHash, new Map(mapPath));
				Console.WriteLine("done");
				return true;
			}
			catch (WebException e)
			{
				Log.Write("debug", "Could not download map '{0}'", mapHash);
				Log.Write("debug", e.ToString());
				return false;
			}
		}
	}
}
