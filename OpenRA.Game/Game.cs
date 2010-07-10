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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
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

		public static World world;
		internal static Viewport viewport;
		public static Controller controller;
		internal static Chrome chrome;
		internal static UserSettings Settings;

		internal static OrderManager orderManager;

		public static bool skipMakeAnims = true;

		public static XRandom CosmeticRandom = new XRandom();	// not synced

		internal static Renderer renderer;
		static int2 clientSize;
		static string mapName;
		public static Session LobbyInfo = new Session();
		static bool packageChangePending;
		static bool mapChangePending;
		static Pair<Assembly, string>[] ModAssemblies;

		static internal bool scrollUp = false;
		static internal bool scrollDown = false;
		static internal bool scrollLeft = false;
		static internal bool scrollRight = false;

		static void LoadModPackages(Manifest manifest)
		{
			FileSystem.UnmountAll();
			Timer.Time("reset: {0}");

			foreach (var dir in manifest.Folders) FileSystem.Mount(dir);
			foreach (var pkg in manifest.Packages) FileSystem.Mount(pkg);

			Timer.Time("mount temporary packages: {0}");
		}

		public static void LoadModAssemblies(Manifest m)
		{
			// All the core namespaces
			var asms = typeof(Game).Assembly.GetNamespaces()
				.Select(c => Pair.New(typeof(Game).Assembly, c))
				.ToList();

			// Namespaces from each mod assembly
			foreach (var a in m.Assemblies)
			{
				var asm = Assembly.LoadFile(Path.GetFullPath(a));
				asms.AddRange(asm.GetNamespaces().Select(ns => Pair.New(asm, ns)));
			}

			ModAssemblies = asms.ToArray();
		}

		public static T CreateObject<T>(string classname)
		{
			foreach (var mod in ModAssemblies)
			{
				var fullTypeName = mod.Second + "." + classname;
				var obj = mod.First.CreateInstance(fullTypeName);
				if (obj != null)
					return (T)obj;
			}

			throw new InvalidOperationException("Cannot locate type: {0}".F(classname));
		}

		public static Dictionary<string, MapStub> AvailableMaps;

		// TODO: Do this nicer
		static Dictionary<string, MapStub> FindMaps(string[] mods)
		{
			var paths = new[] { "maps/" }.Concat(mods.Select(m => "mods/" + m + "/maps/"))
				.Where(p => Directory.Exists(p))
				.SelectMany(p => Directory.GetDirectories(p)).ToList();

			return paths.Select(p => new MapStub(new Folder(p))).ToDictionary(m => m.Uid);
		}

		static void ChangeMods()
		{
			Timer.Time("----ChangeMods");
			var manifest = new Manifest(LobbyInfo.GlobalSettings.Mods);
			Timer.Time("manifest: {0}");
			LoadModAssemblies(manifest);
			SheetBuilder.Initialize(renderer);
			LoadModPackages(manifest);
			Timer.Time("load assemblies, packages: {0}");
			packageChangePending = false;
		}

		static void LoadMap(string mapName)
		{
			Timer.Time("----LoadMap");
			SheetBuilder.Initialize(renderer);
			var manifest = new Manifest(LobbyInfo.GlobalSettings.Mods);
			Timer.Time("manifest: {0}");

			if (!Game.AvailableMaps.ContainsKey(mapName))
				throw new InvalidDataException("Cannot find map with Uid {0}".F(mapName));

			var map = new Map(Game.AvailableMaps[mapName].Package);

			viewport = new Viewport(clientSize, map.TopLeft, map.BottomRight, renderer);
			world = null;	// trying to access the old world will NRE, rather than silently doing it wrong.
			ChromeProvider.Initialize(manifest.Chrome);
			Timer.Time("viewport, ChromeProvider: {0}");
			world = new World(manifest, map);
			Timer.Time("world: {0}");

			SequenceProvider.Initialize(manifest.Sequences, map.Theater);
			Timer.Time("ChromeProv, SeqProv: {0}");

			chrome = new Chrome(renderer, manifest);
			Timer.Time("chrome: {0}");

			Timer.Time("----end LoadMap");
			Debug("Map change {0} -> {1}".F(Game.mapName, mapName));
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

			orderManager = new OrderManager(new NetworkConnection(host, port), ChooseReplayFilename());
		}

		static string ChooseReplayFilename()
		{
			return DateTime.UtcNow.ToString("OpenRA-yyyy-MM-ddThhmmssZ.rep");
		}

		static void JoinLocal()
		{
			if (orderManager != null) orderManager.Dispose();
			orderManager = new OrderManager(new EchoConnection());
		}

		static int lastTime = Environment.TickCount;

		static void ResetTimer()
		{
			lastTime = Environment.TickCount;
		}

		internal static int RenderFrame = 0;

		internal static Chat chat = new Chat();

		internal static int LocalTick = 0;
		const int NetTickScale = 3;		// 120ms net tick for 40ms local tick

		static Queue<Pair<int, string>> syncReports = new Queue<Pair<int, string>>();
		const int numSyncReports = 5;

		internal static void UpdateSyncReport()
		{
			if (!Settings.RecordSyncReports)
				return;

			while (syncReports.Count >= numSyncReports) syncReports.Dequeue();
			syncReports.Enqueue(Pair.New(orderManager.FrameNumber, GenerateSyncReport()));
		}

		static string GenerateSyncReport()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Actors:");
			foreach (var a in world.Actors)
				sb.AppendLine("\t {0} {1} {2} ({3})".F(
					a.ActorID,
					a.Info.Name,
					(a.Owner == null) ? "null" : a.Owner.InternalName,
					Sync.CalculateSyncHash(a)));

			sb.AppendLine("Tick Actors:");
			foreach (var a in world.Queries.WithTraitMultiple<object>())
			{
				var sync = Sync.CalculateSyncHash(a.Trait);
				if (sync != 0)
					sb.AppendLine("\t {0} {1} {2} {3} ({4})".F(
						a.Actor.ActorID,
						a.Actor.Info.Name,
						(a.Actor.Owner == null) ? "null" : a.Actor.Owner.InternalName,
						a.Trait.GetType().Name,
						sync));
			}

			return sb.ToString();
		}

		internal static void DumpSyncReport(int frame)
		{
			var f = syncReports.FirstOrDefault(a => a.First == frame);
			if (f == null)
			{
				Log.Write("sync", "No sync report available!");
				return;
			}

			Log.Write("sync", "Sync for net frame {0} -------------", f.First);
			Log.Write("sync", "{0}", f.Second);
		}

		static void Tick()
		{
			if (packageChangePending)
			{
				// TODO: Only do this on mod change
				Timer.Time("----begin maplist");
				AvailableMaps = FindMaps(LobbyInfo.GlobalSettings.Mods);
				Timer.Time("maplist: {0}");
				ChangeMods();
				return;
			}

			if (mapChangePending)
			{
				mapName = LobbyInfo.GlobalSettings.Map;
				mapChangePending = false;
				return;
			}

			int t = Environment.TickCount;
			int dt = t - lastTime;
			if (dt >= Settings.Timestep)
			{
				using (new PerfSample("tick_time"))
				{
					lastTime += Settings.Timestep;
					chrome.Tick(world);

					orderManager.TickImmediate(world);

					var isNetTick = LocalTick % NetTickScale == 0;

					if (!isNetTick || orderManager.IsReadyForNextFrame)
					{
						++LocalTick;

						if (isNetTick) orderManager.Tick(world);

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

			if (scrollUp == true)
				viewport.Scroll(new float2(0, -10));
			if (scrollRight == true)
				viewport.Scroll(new float2(10, 0));
			if (scrollDown == true)
				viewport.Scroll(new float2(0, 10));
			if (scrollLeft == true)
				viewport.Scroll(new float2(-10, 0));

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
			var oldLobbyInfo = LobbyInfo;

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
			}

			LobbyInfo = session;

			if (!world.GameHasStarted)
				world.SharedRandom = new OpenRA.Thirdparty.Random(LobbyInfo.GlobalSettings.RandomSeed);

			if (orderManager.Connection.ConnectionState == ConnectionState.Connected)
				world.SetLocalPlayer(orderManager.Connection.LocalClientId);

			if (orderManager.FramesAhead != LobbyInfo.GlobalSettings.OrderLatency
				&& !orderManager.GameStarted)
			{
				orderManager.FramesAhead = LobbyInfo.GlobalSettings.OrderLatency;
				Debug("Order lag is now {0} frames.".F(LobbyInfo.GlobalSettings.OrderLatency));
			}

			if (mapName != LobbyInfo.GlobalSettings.Map)
				mapChangePending = true;

			if (string.Join(",", oldLobbyInfo.GlobalSettings.Mods)
				!= string.Join(",", LobbyInfo.GlobalSettings.Mods))
			{
				Debug("Mods list changed, reloading: {0}".F(string.Join(",", LobbyInfo.GlobalSettings.Mods)));
				packageChangePending = true;
			}

			LobbyInfoChanged();
		}

		public static void IssueOrder(Order o) { orderManager.IssueOrder(o); }	/* avoid exposing the OM to mod code */

		static void LoadShellMap(string map)
		{
			LoadMap(map);
			world.Queries = new World.AllQueries(world);

			foreach (var gs in world.WorldActor.traits.WithInterface<IGameStarted>())
				gs.GameStarted(world);
			orderManager.StartGame();
		}

		internal static void StartGame()
		{
			LoadMap(LobbyInfo.GlobalSettings.Map);
			if (orderManager.GameStarted) return;
			Chrome.selectedWidget = null;
			
			world.Queries = new World.AllQueries(world);

			foreach (var gs in world.WorldActor.traits.WithInterface<IGameStarted>())
				gs.GameStarted(world);

			viewport.GoToStartLocation(world.LocalPlayer);
			orderManager.StartGame();
		}

		public static Stance ChooseInitialStance(Player p, Player q)
		{
			if (p == q) return Stance.Ally;

			// Hack: All map players are neutral wrt everyone else
			if (p.Index < 0 || q.Index < 0) return Stance.Neutral;

			var pc = GetClientForPlayer(p);
			var qc = GetClientForPlayer(q);

			return pc.Team != 0 && pc.Team == qc.Team
				? Stance.Ally : Stance.Enemy;
		}

		static Session.Client GetClientForPlayer(Player p)
		{
			return LobbyInfo.Clients.Single(c => c.Index == p.Index);
		}

		static int2 lastPos;
		public static void DispatchMouseInput(MouseInputEvent ev, MouseEventArgs e, Modifiers modifierKeys)
		{
			int sync = world.SyncHash();
			var initialWorld = world;

			if (ev == MouseInputEvent.Down)
				lastPos = new int2(e.Location);

			if (ev == MouseInputEvent.Move &&
				(e.Button == MouseButtons.Middle ||
				e.Button == (MouseButtons.Left | MouseButtons.Right)))
			{
				var p = new int2(e.Location);
				viewport.Scroll(lastPos - p);
				lastPos = p;
			}

			viewport.DispatchMouseInput(world,
				new MouseInput
				{
					Button = (MouseButton)(int)e.Button,
					Event = ev,
					Location = new int2(e.Location),
					Modifiers = modifierKeys,
				});

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

		static Dictionary<char, char> RemapKeys = new Dictionary<char, char>
		{
			{ '!', '1' },
			{ '@', '2' },
			{ '#', '3' },
			{ '$', '4' },
			{ '%', '5' },
			{ '^', '6' },
			{ '&', '7' },
			{ '*', '8' },
			{ '(', '9' },
			{ ')', '0' },
		};

		public static void HandleKeyPress(KeyPressEventArgs e, Modifiers modifiers)
		{
			int sync = world.SyncHash();
			
			if (chrome.HandleKeyPress(e, modifiers))
				return;

			var c = RemapKeys.ContainsKey(e.KeyChar) ? RemapKeys[e.KeyChar] : e.KeyChar;

			if (c >= '0' && c <= '9')
				Game.controller.selection.DoControlGroup(world,
					c - '0', modifiers);

			if (c == 08)
				Game.controller.GotoNextBase();

			if (c == 09)
				BuildPaletteWidget.TabChange((Control.ModifierKeys & Keys.Shift) == Keys.Shift ? true : false);

			// Todo: move this into the widget
			BuildPaletteWidget.DoBuildingHotkey(c, world);
		

			if (sync != Game.world.SyncHash())
				throw new InvalidOperationException("Desync in OnKeyPress");
		}

		public static void HandleArrowKeyScroll(String k, Boolean pressed)
		{
			if (k == "up")
			{
				scrollUp = pressed;
			}
			if (k == "left")
			{
				scrollLeft = pressed;
			}
			if (k == "down")
			{
				scrollDown = pressed;
			}
			if (k == "right")
			{
				scrollRight = pressed;
			}
		}

		public static void HandleModifierKeys(Modifiers mods)
		{
			controller.SetModifiers(mods);
		}

		static Size GetResolution(Settings settings, WindowMode windowmode)
		{
			var desktopResolution = Screen.PrimaryScreen.Bounds.Size;
			var customSize = (windowmode == WindowMode.Windowed) ? Settings.WindowedSize : Settings.FullscreenSize;
			
			if (customSize.X > 0 && customSize.Y > 0)
			{
				desktopResolution.Width = customSize.X;
				desktopResolution.Height = customSize.Y;
			}
			return new Size(
				desktopResolution.Width,
				desktopResolution.Height);
		}

		internal static void Initialize(Settings settings)
		{
			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;
			
			var defaultSupport = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
												+ Path.DirectorySeparatorChar + "OpenRA";
			
			SupportDir = settings.GetValue("SupportDir", defaultSupport);
			Settings = new UserSettings(settings);
			
			Log.LogPath = SupportDir + "Logs" + Path.DirectorySeparatorChar;
			Log.AddChannel("perf", "perf.log", false, false);
			Log.AddChannel("debug", "debug.log", false, false);
			Log.AddChannel("sync", "syncreport.log", true, true);

			LobbyInfo.GlobalSettings.Mods = Settings.InitialMods;

			// Load the default mod to access required files
			LoadModPackages(new Manifest(LobbyInfo.GlobalSettings.Mods));

			Renderer.SheetSize = Settings.SheetSize;

			var resolution = GetResolution(settings, Game.Settings.WindowMode);
			renderer = new Renderer(resolution, Game.Settings.WindowMode);
			resolution = renderer.Resolution;

			controller = new Controller();
			clientSize = new int2(resolution);

			Sound.Initialize();
			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;
			PerfHistory.items["text"].hasNormalTick = false;
			PerfHistory.items["cursor"].hasNormalTick = false;
			AvailableMaps = FindMaps(LobbyInfo.GlobalSettings.Mods);

			ChangeMods();

			if (Settings.Replay != null)
				orderManager = new OrderManager(new ReplayConnection(Settings.Replay));
			else
				JoinLocal();

			LoadShellMap(new Manifest(LobbyInfo.GlobalSettings.Mods).ShellmapUid);

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

		public static void Debug(string s) { chat.AddLine(Color.White, "Debug", s); }

		public static void Disconnect()
		{
			var shellmap = new Manifest(LobbyInfo.GlobalSettings.Mods).ShellmapUid;
			LobbyInfo = new Session();
			LobbyInfo.GlobalSettings.Mods = Settings.InitialMods;
			JoinLocal();
			LoadShellMap(shellmap);

			Chrome.rootWidget.CloseWindow();
			Chrome.rootWidget.OpenWindow("MAINMENU_BG");
		}
		
		static string baseSupportDir = null;
		public static string SupportDir
		{
			set {
				var dir = value;
				
				// Expand paths relative to the personal directory
				if (dir.ElementAt(0) == '~')
					dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + dir.Substring(1);
				
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				
				baseSupportDir = dir + Path.DirectorySeparatorChar;
			}
			get {return baseSupportDir;}
		}


		internal static int GetGameId()
		{
			try
			{
				string s = File.ReadAllText(SupportDir + "currentgameid");
				return int.Parse(s);
			}
			catch (Exception)
			{
				return 0;
			}
		}

		internal static void SetGameId(int id)
		{
			var file = File.CreateText(SupportDir + "currentgameid");
			file.Write(id);
			file.Flush();
			file.Close();
		}
	}
}
