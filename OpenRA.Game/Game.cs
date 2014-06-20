#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using MaxMind.GeoIP2;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA
{
	public static class Game
	{
		public static MouseButtonPreference mouseButtonPreference = new MouseButtonPreference();

		public static ModData modData;
		public static Settings Settings;
		static WorldRenderer worldRenderer;

		internal static OrderManager orderManager;
		static Server.Server server;

		public static MersenneTwister CosmeticRandom = new MersenneTwister(); // not synced

		public static Renderer Renderer;
		public static bool HasInputFocus = false;

		public static DatabaseReader GeoIpDatabase;

		public static OrderManager JoinServer(string host, int port, string password)
		{
			var om = new OrderManager(host, port, password,
				new ReplayRecorderConnection(new NetworkConnection(host, port), ChooseReplayFilename));
			JoinInner(om);
			return om;
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
			JoinInner(new OrderManager("<no server>", -1, "", new ReplayConnection(replayFile)));
		}

		static void JoinLocal()
		{
			JoinInner(new OrderManager("<no server>", -1, "", new EchoConnection()));
		}

		public static int RenderFrame = 0;
		public static int NetFrameNumber { get { return orderManager.NetFrameNumber; } }
		public static int LocalTick { get { return orderManager.LocalFrameNumber; } }
		public const int NetTickScale = 3; // 120 ms net tick for 40 ms local tick
		public const int Timestep = 40;
		public const int TimestepJankThreshold = 250; // Don't catch up for delays larger than 250ms

		public static event Action<OrderManager> ConnectionStateChanged = _ => { };
		static ConnectionState lastConnectionState = ConnectionState.PreConnecting;
		public static int LocalClientId { get { return orderManager.Connection.LocalClientId; } }

		// Hacky workaround for orderManager visibility
		public static Widget OpenWindow(World world, string widget)
		{
			return Ui.OpenWindow(widget, new WidgetArgs() { { "world", world }, { "orderManager", orderManager }, { "worldRenderer", worldRenderer } });
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
			return modData.WidgetLoader.LoadWidget(new WidgetArgs(args)
			{
				{ "world", world },
				{ "orderManager", orderManager },
				{ "worldRenderer", worldRenderer },
			}, parent, id);
		}

		// Note: These delayed actions should only be used by widgets or disposing objects
		// - things that depend on a particular world should be queuing them on the worldactor.
		static ActionQueue delayedActions = new ActionQueue();
		public static void RunAfterTick(Action a) { delayedActions.Add(a); }
		public static void RunAfterDelay(int delay, Action a) { delayedActions.Add(a, delay); }

		static float cursorFrame = 0f;
		static void Tick(OrderManager orderManager)
		{
			if (orderManager.Connection.ConnectionState != lastConnectionState)
			{
				lastConnectionState = orderManager.Connection.ConnectionState;
				ConnectionStateChanged(orderManager);
			}

			TickInner(orderManager);
			if (worldRenderer != null && orderManager.world != worldRenderer.world)
				TickInner(worldRenderer.world.orderManager);

			using (new PerfSample("render"))
			{
				++RenderFrame;

				// worldRenderer is null during the initial install/download screen
				if (worldRenderer != null)
				{
					Renderer.BeginFrame(worldRenderer.Viewport.TopLeft, worldRenderer.Viewport.Zoom);
					Sound.SetListenerPosition(worldRenderer.Position(worldRenderer.Viewport.CenterLocation));
					worldRenderer.Draw();
				}
				else
					Renderer.BeginFrame(int2.Zero, 1f);

				using (new PerfSample("render_widgets"))
				{
					Ui.Draw();
					if (modData != null && modData.CursorProvider != null)
					{
						var cursorName = Ui.Root.GetCursorOuter(Viewport.LastMousePos) ?? "default";
						modData.CursorProvider.DrawCursor(Renderer, cursorName, Viewport.LastMousePos, (int)cursorFrame);
					}
				}

				using (new PerfSample("render_flip"))
				{
					Renderer.EndFrame(new DefaultInputHandler(orderManager.world));
				}
			}

			PerfHistory.items["render"].Tick();
			PerfHistory.items["batches"].Tick();
			PerfHistory.items["render_widgets"].Tick();
			PerfHistory.items["render_flip"].Tick();

			delayedActions.PerformActions();
		}

		static void TickInner(OrderManager orderManager)
		{
			var tick = Environment.TickCount;

			var world = orderManager.world;
			var uiTickDelta = tick - Ui.LastTickTime;
			if (uiTickDelta >= Timestep)
			{
				// Explained below for the world tick calculation
				var integralTickTimestep = (uiTickDelta / Timestep) * Timestep;
				Ui.LastTickTime += integralTickTimestep >= TimestepJankThreshold ? integralTickTimestep : Timestep;

				Viewport.TicksSinceLastMove += uiTickDelta / Timestep;

				Sync.CheckSyncUnchanged(world, Ui.Tick);
				cursorFrame += 0.5f;
			}

			var worldTimestep = world == null ? Timestep : world.Timestep;
			var worldTickDelta = (tick - orderManager.LastTickTime);
			if (worldTimestep != 0 && worldTickDelta >= worldTimestep)
				using (new PerfSample("tick_time"))
				{
					// Tick the world to advance the world time to match real time:
					//    If dt < TickJankThreshold then we should try and catch up by repeatedly ticking
					//    If dt >= TickJankThreshold then we should accept the jank and progress at the normal rate
					// dt is rounded down to an integer tick count in order to preserve fractional tick components.

					var integralTickTimestep = (worldTickDelta / worldTimestep) * worldTimestep;
					orderManager.LastTickTime += integralTickTimestep >= TimestepJankThreshold ? integralTickTimestep : worldTimestep;

					Sound.Tick();
					Sync.CheckSyncUnchanged(world, orderManager.TickImmediate);

					if (world != null)
					{
						var isNetTick = LocalTick % NetTickScale == 0;

						if (!isNetTick || orderManager.IsReadyForNextFrame)
						{
							++orderManager.LocalFrameNumber;

							Log.Write("debug", "--Tick: {0} ({1})", LocalTick, isNetTick ? "net" : "local");

							if (isNetTick)
								orderManager.Tick();

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

						Sync.CheckSyncUnchanged(world, () => world.TickRender(worldRenderer));
					}
				}
		}

		public static event Action LobbyInfoChanged = () => { };

		internal static void SyncLobbyInfo()
		{
			LobbyInfoChanged();
		}

		public static event Action BeforeGameStart = () => { };
		internal static void StartGame(string mapUID, bool isShellmap)
		{
			BeforeGameStart();

			Map map;

			using (new PerfTimer("PrepareMap"))
				map = modData.PrepareMap(mapUID);
			using (new PerfTimer("NewWorld"))
			{
				orderManager.world = new World(map, orderManager, isShellmap);
				orderManager.world.Timestep = Timestep;
			}
			worldRenderer = new WorldRenderer(orderManager.world);
			using (new PerfTimer("LoadComplete"))
				orderManager.world.LoadComplete(worldRenderer);

			if (orderManager.GameStarted)
				return;

			Ui.MouseFocusWidget = null;
			Ui.KeyboardFocusWidget = null;

			orderManager.LocalFrameNumber = 0;
			orderManager.LastTickTime = Environment.TickCount;
			orderManager.StartGame();
			worldRenderer.RefreshPalette();
		}

		public static bool IsHost
		{
			get
			{
				var id = orderManager.Connection.LocalClientId;
				var client = orderManager.LobbyInfo.ClientWithIndex(id);
				return client != null && client.IsAdmin;
			}
		}

		static Modifiers modifiers;
		public static Modifiers GetModifierKeys() { return modifiers; }
		internal static void HandleModifierKeys(Modifiers mods) { modifiers = mods; }

		internal static void Initialize(Arguments args)
		{
			Console.WriteLine("Platform is {0}", Platform.CurrentPlatform);

			AppDomain.CurrentDomain.AssemblyResolve += GlobalFileSystem.ResolveAssembly;

			Settings = new Settings(Platform.SupportDir + "settings.yaml", args);

			Log.LogPath = Platform.SupportDir + "Logs" + Path.DirectorySeparatorChar;
			Log.AddChannel("perf", "perf.log");
			Log.AddChannel("debug", "debug.log");
			Log.AddChannel("sync", "syncreport.log");
			Log.AddChannel("server", "server.log");
			Log.AddChannel("sound", "sound.log");
			Log.AddChannel("graphics", "graphics.log");
			Log.AddChannel("geoip", "geoip.log");

			if (Settings.Server.DiscoverNatDevices)
				UPnP.TryNatDiscovery();
			else
			{
				Settings.Server.NatDeviceAvailable = false;
				Settings.Server.AllowPortForward = false;
			}

			try
			{
				GeoIpDatabase = new DatabaseReader("GeoLite2-Country.mmdb");
			}
			catch (Exception e)
			{
				Log.Write("geoip", "DatabaseReader failed: {0}", e);
			}

			GlobalFileSystem.Mount("."); // Needed to access shaders
			var renderers = new[] { Settings.Graphics.Renderer, "Sdl2", null };
			foreach (var r in renderers)
			{
				if (r == null)
					throw new InvalidOperationException("No suitable renderers were found. Check graphics.log for details.");

				Settings.Graphics.Renderer = r;
				try
				{
					Renderer.Initialize(Settings.Graphics.Mode);
					break;
				}
				catch (Exception e)
				{
					Log.Write("graphics", "{0}", e);
					Console.WriteLine("Renderer initialization failed. Fallback in place. Check graphics.log for details.");
				}
			}

			Renderer = new Renderer();

			try
			{
				Sound.Create(Settings.Sound.Engine);
			}
			catch (Exception e)
			{
				Log.Write("sound", "{0}", e);
				Console.WriteLine("Creating the sound engine failed. Fallback in place. Check sound.log for details.");
				Settings.Sound.Engine = "Null";
				Sound.Create(Settings.Sound.Engine);
			}

			Console.WriteLine("Available mods:");
			foreach (var mod in ModMetadata.AllMods)
				Console.WriteLine("\t{0}: {1} ({2})", mod.Key, mod.Value.Title, mod.Value.Version);

			InitializeWithMod(Settings.Game.Mod, args.GetValue("Launch.Replay", null));

			if (Settings.Server.DiscoverNatDevices)
				RunAfterDelay(Settings.Server.NatDiscoveryTimeout, UPnP.StoppingNatDiscovery);
		}

		public static void InitializeWithMod(string mod, string replay)
		{
			// Clear static state if we have switched mods
			LobbyInfoChanged = () => { };
			AddChatLine = (a, b, c) => { };
			ConnectionStateChanged = om => { };
			BeforeGameStart = () => { };
			Ui.ResetAll();

			worldRenderer = null;
			if (server != null)
				server.Shutdown();
			if (orderManager != null)
				orderManager.Dispose();

			// Fall back to default if the mod doesn't exist
			if (!ModMetadata.AllMods.ContainsKey(mod))
				mod = new GameSettings().Mod;

			Console.WriteLine("Loading mod: {0}", mod);
			Settings.Game.Mod = mod;

			Sound.StopMusic();
			Sound.StopVideo();
			Sound.Initialize();

			modData = new ModData(mod);
			Renderer.InitializeFonts(modData.Manifest);
			modData.InitializeLoaders();
			using (new PerfTimer("LoadMaps"))
				modData.MapCache.LoadMaps();

			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;
			PerfHistory.items["render_widgets"].hasNormalTick = false;
			PerfHistory.items["render_flip"].hasNormalTick = false;

			JoinLocal();

			if (Settings.Server.Dedicated)
			{
				while (true)
				{
					Settings.Server.Map = WidgetUtils.ChooseInitialMap(Settings.Server.Map);
					Settings.Save();
					CreateServer(new ServerSettings(Settings.Server));
					while (true)
					{
						System.Threading.Thread.Sleep(100);

						if (server.State == Server.ServerState.GameStarted && server.Conns.Count < 1)
						{
							Console.WriteLine("No one is playing, shutting down...");
							server.Shutdown();
							break;
						}
					}

					if (Settings.Server.DedicatedLoop)
					{
						Console.WriteLine("Starting a new server instance...");
						modData.MapCache.LoadMaps();
						continue;
					}

					break;
				}

				Environment.Exit(0);
			}
			else
			{
				modData.LoadScreen.StartGame();
				Settings.Save();
				if (!string.IsNullOrEmpty(replay))
					Game.JoinReplay(replay);
			}
		}

		public static void LoadShellMap()
		{
			var shellmap = ChooseShellmap();

			using (new PerfTimer("StartGame"))
				StartGame(shellmap, true);
		}

		static string ChooseShellmap()
		{
			var shellmaps = modData.MapCache
				.Where(m => m.Status == MapStatus.Available && m.Map.UseAsShellmap)
				.Select(m => m.Uid);

			if (!shellmaps.Any())
				throw new InvalidDataException("No valid shellmaps available");

			return shellmaps.Random(CosmeticRandom);
		}

		static RunStatus state = RunStatus.Running;
		public static event Action OnQuit = () => { };

		static double idealFrameTime;
		public static void SetIdealFrameTime(int fps)
		{ 
			idealFrameTime = 1.0 / fps;
		}

		internal static RunStatus Run()
		{
			if (Settings.Graphics.MaxFramerate < 1)
			{
				Settings.Graphics.MaxFramerate = new GraphicSettings().MaxFramerate;
				Settings.Graphics.CapFramerate = false;
			}

			SetIdealFrameTime(Settings.Graphics.MaxFramerate);

			while (state == RunStatus.Running)
			{
				if (Settings.Graphics.CapFramerate)
				{
					var sw = Stopwatch.StartNew();

					Tick(orderManager);

					var waitTime = Math.Min(idealFrameTime - sw.Elapsed.TotalSeconds, 1);
					if (waitTime > 0)
						System.Threading.Thread.Sleep(TimeSpan.FromSeconds(waitTime));
				}
				else
					Tick(orderManager);
			}

			// Ensure that the active replay is properly saved
			if (orderManager != null)
				orderManager.Dispose();
				
			Renderer.Device.Dispose();

			OnQuit();

			return state;
		}

		public static void Exit()
		{
			state = RunStatus.Success;
		}

		public static void Restart()
		{
			state = RunStatus.Restart;
		}

		public static Action<Color, string, string> AddChatLine = (c, n, s) => { };

		public static void Debug(string s, params object[] args)
		{
			AddChatLine(Color.White, "Debug", string.Format(s, args));
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

		public static T CreateObject<T>(string name)
		{
			return modData.ObjectCreator.CreateObject<T>(name);
		}

		public static void CreateServer(ServerSettings settings)
		{
			server = new Server.Server(new IPEndPoint(IPAddress.Any, settings.ListenPort), settings, modData);
		}

		public static int CreateLocalServer(string map)
		{
			var settings = new ServerSettings()
			{
				Name = "Skirmish Game",
				Map = map,
				AdvertiseOnline = false,
				AllowPortForward = false
			};

			server = new Server.Server(new IPEndPoint(IPAddress.Loopback, 0), settings, modData);

			return server.Port;
		}

		public static bool IsCurrentWorld(World world)
		{
			return orderManager != null && orderManager.world == world;
		}
	}
}
