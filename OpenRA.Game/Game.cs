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
using OpenRA.Traits;
using OpenRA.Widgets;

using Timer = OpenRA.Support.Timer;
using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA
{
	public static class Game
	{
		public static readonly int CellSize = 24;

		public static ModData modData;
		public static World world;
		public static Viewport viewport;
		public static Settings Settings;

		internal static OrderManager orderManager;

		public static XRandom CosmeticRandom = new XRandom();	// not synced

		public static Renderer Renderer;
		static int2 clientSize;
		public static Session LobbyInfo = new Session();
		
		static void LoadMap(string uid)
		{
			var map = modData.PrepareMap(uid);
			
			viewport = new Viewport(clientSize, map.TopLeft, map.BottomRight, Renderer);
			world = null;	// trying to access the old world will NRE, rather than silently doing it wrong.
			Timer.Time("viewport: {0}");
			world = new World(modData.Manifest, map);
			Timer.Time("world: {0}");
		}

		public static void MoveViewport(int2 loc)
		{
			viewport.Center(loc);
		}

		internal static string CurrentHost = "";
		internal static int CurrentPort = 0;

		internal static void JoinServer(string host, int port)
		{
			if (orderManager != null) orderManager.Dispose();

			CurrentHost = host;
			CurrentPort = port;

			lastConnectionState = ConnectionState.PreConnecting;
			ConnectionStateChanged();

			orderManager = new OrderManager(new NetworkConnection(host, port), ChooseReplayFilename());
		}

		static string ChooseReplayFilename()
		{
			return DateTime.UtcNow.ToString("OpenRA-yyyy-MM-ddThhmmssZ.rep");
		}

		static void JoinLocal()
		{
			lastConnectionState = ConnectionState.PreConnecting;
			ConnectionStateChanged();

			if (orderManager != null) orderManager.Dispose();
			orderManager = new OrderManager(new EchoConnection());
		}

		static int lastTime = Environment.TickCount;

		static void ResetTimer()
		{
			lastTime = Environment.TickCount;
		}

		internal static int RenderFrame = 0;
		internal static int LocalTick = 0;
		const int NetTickScale = 3;		// 120ms net tick for 40ms local tick

		public static event Action ConnectionStateChanged = () => { };
		static ConnectionState lastConnectionState = ConnectionState.PreConnecting;
		public static int LocalClientId { get { return orderManager.Connection.LocalClientId; } }

		static void Tick()
		{
			if (orderManager.Connection.ConnectionState != lastConnectionState)
			{
				lastConnectionState = orderManager.Connection.ConnectionState;
				ConnectionStateChanged();
			}

			int t = Environment.TickCount;
			int dt = t - lastTime;
			if (dt >= Settings.Game.Timestep)
			{
				using (new PerfSample("tick_time"))
				{
					lastTime += Settings.Game.Timestep;
					Widget.DoTick(world);

					orderManager.TickImmediate(world);

					var isNetTick = LocalTick % NetTickScale == 0;

					if (!isNetTick || orderManager.IsReadyForNextFrame)
					{
						++LocalTick;

						if (isNetTick) orderManager.Tick(world);

						world.OrderGenerator.Tick(world);
						world.Selection.Tick(world);
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
				viewport.DrawRegions(world);
				Sound.SetListenerPosition(viewport.Location + .5f * new float2(viewport.Width, viewport.Height));
			}

			PerfHistory.items["render"].Tick();
			PerfHistory.items["batches"].Tick();
			PerfHistory.items["text"].Tick();
			PerfHistory.items["cursor"].Tick();

			MasterServerQuery.Tick();
		}

		internal static event Action LobbyInfoChanged = () => { };

		internal static void SyncLobbyInfo(string data)
		{
			LobbyInfo = Session.Deserialize(data);

			if( !world.GameHasStarted )
				world.SharedRandom = new XRandom( LobbyInfo.GlobalSettings.RandomSeed );

			if (orderManager.Connection.ConnectionState == ConnectionState.Connected)
				world.SetLocalPlayer(orderManager.Connection.LocalClientId);

			if (orderManager.FramesAhead != LobbyInfo.GlobalSettings.OrderLatency
				&& !orderManager.GameStarted)
			{
				orderManager.FramesAhead = LobbyInfo.GlobalSettings.OrderLatency;
				Debug("Order lag is now {0} frames.".F(LobbyInfo.GlobalSettings.OrderLatency));
			}

			LobbyInfoChanged();
		}

		public static void IssueOrder(Order o) { orderManager.IssueOrder(o); }	/* avoid exposing the OM to mod code */


		public static event Action AfterGameStart = () => {};
		public static event Action BeforeGameStart = () => {};
		internal static void StartGame(string map)
		{
			BeforeGameStart();
			LoadMap(map);
			if (orderManager.GameStarted) return;
			Widget.SelectedWidget = null;

			world.Queries = new World.AllQueries(world);

			foreach (var gs in world.WorldActor.TraitsImplementing<IGameStarted>())
				gs.GameStarted(world);

			orderManager.StartGame();
			viewport.RefreshPalette();
			AfterGameStart();
		}

		public static void DispatchMouseInput(MouseInputEvent ev, MouseEventArgs e, Modifiers modifierKeys)
		{
			int sync = world.SyncHash();
			var initialWorld = world;

			var mi = new MouseInput
			{
				Button = (MouseButton)(int)e.Button,
				Event = ev,
				Location = new int2(e.Location),
				Modifiers = modifierKeys,
			};
			Widget.HandleInput(world, mi);

			if (sync != world.SyncHash() && world == initialWorld)
				throw new InvalidOperationException("Desync in DispatchMouseInput");
		}

		internal static bool IsHost
		{
			get { return orderManager.Connection.LocalClientId == 0; }
		}

		internal static Session.Client LocalClient
		{
			get { return LobbyInfo.Clients.FirstOrDefault(c => c.Index == orderManager.Connection.LocalClientId); }
		}

		public static void HandleKeyEvent(KeyInput e)
		{
			int sync = world.SyncHash();

			if (Widget.HandleKeyPress(e))
				return;

			if (sync != Game.world.SyncHash())
				throw new InvalidOperationException("Desync in OnKeyPress");
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

			Log.LogPath = SupportDir + "Logs" + Path.DirectorySeparatorChar;
			Log.AddChannel("perf", "perf.log");
			Log.AddChannel("debug", "debug.log");
			Log.AddChannel("sync", "syncreport.log");

			LobbyInfo.GlobalSettings.Mods = Settings.Game.Mods;
			modData = new ModData( LobbyInfo.GlobalSettings.Mods );

			Renderer.SheetSize = Settings.Game.SheetSize;

			Renderer.Initialize( Game.Settings.Graphics.Mode );

			Sound.Initialize();
			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;
			PerfHistory.items["text"].hasNormalTick = false;
			PerfHistory.items["cursor"].hasNormalTick = false;

			Renderer = new Renderer();
			clientSize = new int2(Renderer.Resolution);

			JoinLocal();
			StartGame(modData.Manifest.ShellmapUid);

			ResetTimer();
		}

		static bool quit;
		internal static void Run()
		{
			while (!quit)
			{
				Tick();
				Application.DoEvents();
			}
		}

		public static void Exit() { quit = true; }

		public static Action<Color,string,string> AddChatLine = (c,n,s) => {};

		public static void Debug(string s)
		{
			AddChatLine(Color.White, "Debug", s); 
		}

		public static void Disconnect()
		{
			orderManager.Dispose();
			var shellmap = modData.Manifest.ShellmapUid;
			LobbyInfo = new Session();
			LobbyInfo.GlobalSettings.Mods = Settings.Game.Mods;
			JoinLocal();
			StartGame(shellmap);

			Widget.RootWidget.CloseWindow();
			Widget.RootWidget.OpenWindow("MAINMENU_BG");
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
