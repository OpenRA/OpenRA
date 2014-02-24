#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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

using XRandom = OpenRA.Thirdparty.Random;

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

		public static XRandom CosmeticRandom = new XRandom();	// not synced

		public static Renderer Renderer;
		public static bool HasInputFocus = false;

		public static void JoinServer(string host, int port, string password)
		{
			JoinInner(new OrderManager(host, port, password,
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
			JoinInner(new OrderManager("<no server>", -1, "", new ReplayConnection(replayFile)));
		}

		static void JoinLocal()
		{
			JoinInner(new OrderManager("<no server>", -1, "", new EchoConnection()));
		}

		public static int RenderFrame = 0;
		public static int NetFrameNumber { get { return orderManager.NetFrameNumber; } }
		public static int LocalTick { get { return orderManager.LocalFrameNumber; } }
		public const int NetTickScale = 3;		// 120ms net tick for 40ms local tick

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
					Renderer.BeginFrame(worldRenderer.Viewport.TopLeft.ToFloat2(), worldRenderer.Viewport.Zoom);
					Sound.SetListenerPosition(worldRenderer.Position(worldRenderer.Viewport.CenterLocation));
					worldRenderer.Draw();
				}
				else
					Renderer.BeginFrame(float2.Zero, 1f);

				using (new PerfSample("render_widgets"))
				{
					Ui.Draw();
					var cursorName = Ui.Root.GetCursorOuter(Viewport.LastMousePos) ?? "default";
					CursorProvider.DrawCursor(Renderer, cursorName, Viewport.LastMousePos, (int)cursorFrame);
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
			int t = Environment.TickCount;
			int dt = t - orderManager.LastTickTime;
			if (dt >= Settings.Game.Timestep)
				using (new PerfSample("tick_time"))
				{
					orderManager.LastTickTime += Settings.Game.Timestep;
					Ui.Tick();
					var world = orderManager.world;
					if (orderManager.GameStarted)
						++Viewport.TicksSinceLastMove;

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

						cursorFrame += 0.5f;
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

			var map = modData.PrepareMap(mapUID);
			orderManager.world = new World(modData.Manifest, map, orderManager, isShellmap);
			worldRenderer = new WorldRenderer(orderManager.world);
			orderManager.world.LoadComplete(worldRenderer);

			if (orderManager.GameStarted)
				return;

			Ui.MouseFocusWidget = null;
			Ui.KeyboardFocusWidget = null;

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

			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;

			Settings = new Settings(Platform.SupportDir + "settings.yaml", args);

			Log.LogPath = Platform.SupportDir + "Logs" + Path.DirectorySeparatorChar;
			Log.AddChannel("perf", "perf.log");
			Log.AddChannel("debug", "debug.log");
			Log.AddChannel("sync", "syncreport.log");
			Log.AddChannel("server", "server.log");
			Log.AddChannel("sound", "sound.log");
			Log.AddChannel("graphics", "graphics.log");

			if (Settings.Server.DiscoverNatDevices)
				UPnP.TryNatDiscovery();
			else
			{
				Settings.Server.NatDeviceAvailable = false;
				Settings.Server.AllowPortForward = false;
			}

			FileSystem.Mount("."); // Needed to access shaders
			var renderers = new[] { Settings.Graphics.Renderer, "Sdl2", "Gl", "Cg", null };
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
			foreach (var mod in Mod.AllMods)
				Console.WriteLine("\t{0}: {1} ({2})", mod.Key, mod.Value.Title, mod.Value.Version);

			InitializeWithMod(Settings.Game.Mod, args.GetValue("Launch.Replay", null));

			if (Settings.Server.DiscoverNatDevices)
				RunAfterDelay(Settings.Server.NatDiscoveryTimeout, UPnP.TryStoppingNatDiscovery);
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
			if (!Mod.AllMods.ContainsKey(mod))
				mod = new GameSettings().Mod;

			Console.WriteLine("Loading mod: {0}", mod);
			Settings.Game.Mod = mod;

			Sound.StopMusic();
			Sound.StopVideo();
			Sound.Initialize();

			modData = new ModData(mod);
			Renderer.InitializeFonts(modData.Manifest);
			modData.InitializeLoaders();

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

						if (server.State == Server.ServerState.GameStarted && server.Conns.Count <= 1)
						{
							Console.WriteLine("No one is playing, shutting down...");
							server.Shutdown();
							break;
						}
					}

					if (Settings.Server.DedicatedLoop)
					{
						Console.WriteLine("Starting a new server instance...");
						modData.InitializeLoaders();
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
			StartGame(ChooseShellmap(), true);
		}

		static string ChooseShellmap()
		{
			var shellmaps = modData.AvailableMaps
				.Where(m => m.Value.UseAsShellmap);

			if (!shellmaps.Any())
				throw new InvalidDataException("No valid shellmaps available");

			return shellmaps.Random(CosmeticRandom).Key;
		}

		static bool quit;
		public static event Action OnQuit = () => { };

		static double idealFrameTime;
		public static void SetIdealFrameTime(int fps)
		{ 
			idealFrameTime = 1.0 / fps;
		}

		internal static void Run()
		{
			if (Settings.Graphics.MaxFramerate < 1)
			{
				Settings.Graphics.MaxFramerate = new GraphicSettings().MaxFramerate;
				Settings.Graphics.CapFramerate = false;
			}

			SetIdealFrameTime(Settings.Graphics.MaxFramerate);

			while (!quit)
			{
				if (Settings.Graphics.CapFramerate)
				{
					var sw = new Stopwatch();

					Tick(orderManager);

					var waitTime = Math.Min(idealFrameTime - sw.ElapsedTime(), 1);
					if (waitTime > 0)
						System.Threading.Thread.Sleep(TimeSpan.FromSeconds(waitTime));
				}
				else
					Tick(orderManager);
			}

			OnQuit();
		}

		public static void Exit() { quit = true; }

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

		public static bool DownloadMap(string mapHash)
		{
			var mod = Game.modData.Manifest.Mod;
			var dirPath = new[] { Platform.SupportDir, "maps", mod.Id }.Aggregate(Path.Combine);
			var tempFile = Path.Combine(dirPath, Path.GetRandomFileName());
			if (!Directory.Exists(dirPath))
				Directory.CreateDirectory(dirPath);
			foreach (var MapRepository in Game.Settings.Game.MapRepositories)
			{
				try
				{
					var url = MapRepository + mapHash;

					var request = WebRequest.Create(url);
					request.Method = "HEAD";
					var res = request.GetResponse();

					if (res.Headers["Content-Disposition"] == null)
						continue;
					var mapPath = Path.Combine(dirPath, res.Headers ["Content-Disposition"].Replace("attachment; filename = ", ""));
					Log.Write("debug", "Trying to download map to '{0}' using {1}", mapPath, MapRepository);

					WebClient webClient = new WebClient();
					webClient.DownloadFile(url, tempFile);
					File.Move(tempFile, mapPath);
					Game.modData.AvailableMaps.Add(mapHash, new Map(mapPath));
					Log.Write("debug", "New map has been downloaded to '{0}'", mapPath);

					return true;
				} 
				catch (WebException e)
				{
					Log.Write("debug", "Could not download map '{0}' using {1}", mapHash, MapRepository);
					Log.Write("debug", e.ToString());
					File.Delete(tempFile);
					continue;
				}
			}
			return false;
		}
	}
}
