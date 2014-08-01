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
using System.Collections.Generic;
using System.IO;
using OpenRA.Graphics;

namespace OpenRA
{
	public enum MouseScrollType { Disabled, Standard, Inverted }

	public class ServerSettings
	{
		public string Name = "OpenRA Game";
		public int ListenPort = 1234;
		public int ExternalPort = 1234;
		public bool AdvertiseOnline = true;
		public string Password = "";
		public string MasterServer = "http://master.openra.net/";
		public bool DiscoverNatDevices = false; // Allow users to disable NAT discovery if problems occur
		public bool AllowPortForward = true; // let the user disable it even if compatible devices are found
		public bool NatDeviceAvailable = false; // internal check if discovery succeeded
		public int NatDiscoveryTimeout = 1000; // ms to search for UPnP enabled NATs
		public bool VerboseNatDiscovery = false; // print very detailed logs for debugging
		public bool AllowCheats = false;
		public string Map = null;
		public string[] Ban = { };
		public int TimeOut = 0;
		public bool Dedicated = false;
		public bool DedicatedLoop = true;
		public bool LockBots = false;
		public bool AllowVersionMismatch = false;

		public ServerSettings() { }

		public ServerSettings(ServerSettings other)
		{
			Name = other.Name;
			ListenPort = other.ListenPort;
			ExternalPort = other.ExternalPort;
			AdvertiseOnline = other.AdvertiseOnline;
			Password = other.Password;
			MasterServer = other.MasterServer;
			DiscoverNatDevices = other.DiscoverNatDevices;
			AllowPortForward = other.AllowPortForward;
			NatDeviceAvailable = other.NatDeviceAvailable;
			NatDiscoveryTimeout = other.NatDiscoveryTimeout;
			VerboseNatDiscovery = other.VerboseNatDiscovery;
			AllowCheats = other.AllowCheats;
			Map = other.Map;
			Ban = other.Ban;
			TimeOut = other.TimeOut;
			Dedicated = other.Dedicated;
			DedicatedLoop = other.DedicatedLoop;
			LockBots = other.LockBots;
			AllowVersionMismatch = other.AllowVersionMismatch;
		}
	}

	public class DebugSettings
	{
		public bool BotDebug = false;
		public bool PerfText = false;
		public bool PerfGraph = false;
		public float LongTickThresholdMs = 1;
		public bool SanityCheckUnsyncedCode = false;
		public int Samples = 25;
		public bool IgnoreVersionMismatch = false;

		public bool ShowFatalErrorDialog = true;
		public string FatalErrorDialogFaq = "http://wiki.openra.net/FAQ";
	}

	public class GraphicSettings
	{
		public string Renderer = "Sdl2";
		public WindowMode Mode = WindowMode.PseudoFullscreen;
		public int2 FullscreenSize = new int2(0, 0);
		public int2 WindowedSize = new int2(1024, 768);
		public bool PixelDouble = false;
		public bool CapFramerate = true;
		public int MaxFramerate = 60;

		public int BatchSize = 8192;
		public int NumTempBuffers = 8;
		public int SheetSize = 2048;

		public string Language = "english";
		public string DefaultLanguage = "english";
	}

	public class SoundSettings
	{
		public float SoundVolume = 0.5f;
		public float MusicVolume = 0.5f;
		public float VideoVolume = 0.5f;

		public bool Shuffle = false;
		public bool Repeat = false;
		public bool MapMusic = true;

		public string Engine = "AL";
		public string Device = null;

		public bool CashTicks = true;
	}

	public class PlayerSettings
	{
		public string Name = "Newbie";
		public HSLColor Color = new HSLColor(75, 255, 180);
		public string LastServer = "localhost:1234";
	}

	public class GameSettings
	{
		public string Mod = "modchooser";
		public string PreviousMod = "ra";

		public bool ShowShellmap = true;

		public bool ViewportEdgeScroll = true;
		public bool LockMouseWindow = false;
		public MouseScrollType MouseScroll = MouseScrollType.Standard;
		public float ViewportEdgeScrollStep = 10f;
		public float UIScrollSpeed = 50f;

		public bool UseClassicMouseStyle = false;
		public bool AlwaysShowStatusBars = false;
		public bool TeamHealthColors = false;

		public bool AllowDownloading = true;
		public string MapRepository = "http://resource.openra.net/map/";

		public bool FetchNews = true;
		public string NewsUrl = "http://www.openra.net/gamenews";
		public DateTime NewsFetchedDate;
	}

	public class KeySettings
	{
		public Hotkey CycleBaseKey = new Hotkey(Keycode.BACKSPACE, Modifiers.None);
		public Hotkey ToLastEventKey = new Hotkey(Keycode.SPACE, Modifiers.None);
		public Hotkey ToSelectionKey = new Hotkey(Keycode.HOME, Modifiers.None);
		public Hotkey SelectAllUnitsKey = new Hotkey(Keycode.A, Modifiers.Ctrl);
		public Hotkey SelectUnitsByTypeKey = new Hotkey(Keycode.T, Modifiers.Ctrl);

		public Hotkey PauseKey = new Hotkey(Keycode.F8, Modifiers.None);
		public Hotkey PlaceBeaconKey = new Hotkey(Keycode.F9, Modifiers.None);
		public Hotkey SellKey = new Hotkey(Keycode.F10, Modifiers.None);
		public Hotkey PowerDownKey = new Hotkey(Keycode.F11, Modifiers.None);
		public Hotkey RepairKey = new Hotkey(Keycode.F12, Modifiers.None);

		public Hotkey NextProductionTabKey = new Hotkey(Keycode.PAGEDOWN, Modifiers.None);
		public Hotkey PreviousProductionTabKey = new Hotkey(Keycode.PAGEUP, Modifiers.None);
		public Hotkey CycleProductionBuildingsKey = new Hotkey(Keycode.TAB, Modifiers.None);

		public Hotkey ToggleStatusBarsKey = new Hotkey(Keycode.INSERT, Modifiers.None);

		public Hotkey AttackMoveKey = new Hotkey(Keycode.A, Modifiers.None);
		public Hotkey StopKey = new Hotkey(Keycode.S, Modifiers.None);
		public Hotkey ScatterKey = new Hotkey(Keycode.X, Modifiers.None);
		public Hotkey DeployKey = new Hotkey(Keycode.F, Modifiers.None);
		public Hotkey StanceCycleKey = new Hotkey(Keycode.Z, Modifiers.None);
		public Hotkey GuardKey = new Hotkey(Keycode.D, Modifiers.None);

		public Hotkey ObserverCombinedView = new Hotkey(Keycode.MINUS, Modifiers.None);
		public Hotkey ObserverWorldView = new Hotkey(Keycode.EQUALS, Modifiers.None);

		public Hotkey TogglePixelDoubleKey = new Hotkey(Keycode.PERIOD, Modifiers.None);

		public Hotkey DevReloadChromeKey = new Hotkey(Keycode.C, Modifiers.Ctrl | Modifiers.Shift);

		public Hotkey Production01Key = new Hotkey(Keycode.F1, Modifiers.None);
		public Hotkey Production02Key = new Hotkey(Keycode.F2, Modifiers.None);
		public Hotkey Production03Key = new Hotkey(Keycode.F3, Modifiers.None);
		public Hotkey Production04Key = new Hotkey(Keycode.F4, Modifiers.None);
		public Hotkey Production05Key = new Hotkey(Keycode.F5, Modifiers.None);
		public Hotkey Production06Key = new Hotkey(Keycode.F6, Modifiers.None);
		public Hotkey Production07Key = new Hotkey(Keycode.F7, Modifiers.None);
		public Hotkey Production08Key = new Hotkey(Keycode.F8, Modifiers.None);
		public Hotkey Production09Key = new Hotkey(Keycode.F9, Modifiers.None);
		public Hotkey Production10Key = new Hotkey(Keycode.F10, Modifiers.None);
		public Hotkey Production11Key = new Hotkey(Keycode.F11, Modifiers.None);
		public Hotkey Production12Key = new Hotkey(Keycode.F12, Modifiers.None);

		public Hotkey Production13Key = new Hotkey(Keycode.F1, Modifiers.Ctrl);
		public Hotkey Production14Key = new Hotkey(Keycode.F2, Modifiers.Ctrl);
		public Hotkey Production15Key = new Hotkey(Keycode.F3, Modifiers.Ctrl);
		public Hotkey Production16Key = new Hotkey(Keycode.F4, Modifiers.Ctrl);
		public Hotkey Production17Key = new Hotkey(Keycode.F5, Modifiers.Ctrl);
		public Hotkey Production18Key = new Hotkey(Keycode.F6, Modifiers.Ctrl);
		public Hotkey Production19Key = new Hotkey(Keycode.F7, Modifiers.Ctrl);
		public Hotkey Production20Key = new Hotkey(Keycode.F8, Modifiers.Ctrl);
		public Hotkey Production21Key = new Hotkey(Keycode.F9, Modifiers.Ctrl);
		public Hotkey Production22Key = new Hotkey(Keycode.F10, Modifiers.Ctrl);
		public Hotkey Production23Key = new Hotkey(Keycode.F11, Modifiers.Ctrl);
		public Hotkey Production24Key = new Hotkey(Keycode.F12, Modifiers.Ctrl);


		public Hotkey GetProductionHotkey(int index)
		{
			var field = GetType().GetField("Production{0:D2}Key".F(index + 1));
			if (field == null)
				return Hotkey.Invalid;

			return (Hotkey)field.GetValue(this);
		}
	}

	public class IrcSettings
	{
		public string Hostname = "irc.openra.net";
		public int Port = 6667;
		public string Nickname = null;
		public string Username = "openra";
		public string Realname = null;
		public string DefaultNickname = "Newbie";
		public string Channel = "global";
		public string TimestampFormat = "HH:mm:ss";
		public int ReconnectDelay = 10000;
		public int ConnectionTimeout = 300000;
		public bool Debug = false;
		public bool ConnectAutomatically = false;
	}

	public class Settings
	{
		string settingsFile;

		public PlayerSettings Player = new PlayerSettings();
		public GameSettings Game = new GameSettings();
		public SoundSettings Sound = new SoundSettings();
		public GraphicSettings Graphics = new GraphicSettings();
		public ServerSettings Server = new ServerSettings();
		public DebugSettings Debug = new DebugSettings();
		public KeySettings Keys = new KeySettings();
		public IrcSettings Irc = new IrcSettings();

		public Dictionary<string, object> Sections;

		public Settings(string file, Arguments args)
		{
			settingsFile = file;
			Sections = new Dictionary<string, object>()
			{
				{ "Player", Player },
				{ "Game", Game },
				{ "Sound", Sound },
				{ "Graphics", Graphics },
				{ "Server", Server },
				{ "Debug", Debug },
				{ "Keys", Keys },
				{ "Irc", Irc }
			};

			// Override fieldloader to ignore invalid entries
			var err1 = FieldLoader.UnknownFieldAction;
			var err2 = FieldLoader.InvalidValueAction;

			FieldLoader.UnknownFieldAction = (s, f) => Console.WriteLine("Ignoring unknown field `{0}` on `{1}`".F(s, f.Name));

			if (File.Exists(settingsFile))
			{
				var yaml = MiniYaml.DictFromFile(settingsFile);

				foreach (var kv in Sections)
					if (yaml.ContainsKey(kv.Key))
						LoadSectionYaml(yaml[kv.Key], kv.Value);
			}

			// Override with commandline args
			foreach (var kv in Sections)
				foreach (var f in kv.Value.GetType().GetFields())
					if (args.Contains(kv.Key + "." + f.Name))
						FieldLoader.LoadField(kv.Value, f.Name, args.GetValue(kv.Key + "." + f.Name, ""));

			FieldLoader.UnknownFieldAction = err1;
			FieldLoader.InvalidValueAction = err2;
		}

		public void Save()
		{
			var root = new List<MiniYamlNode>();
			foreach (var kv in Sections)
				root.Add(new MiniYamlNode(kv.Key, FieldSaver.SaveDifferences(kv.Value, Activator.CreateInstance(kv.Value.GetType()))));

			root.WriteToFile(settingsFile);
		}

		static void LoadSectionYaml(MiniYaml yaml, object section)
		{
			var defaults = Activator.CreateInstance(section.GetType());
			FieldLoader.InvalidValueAction = (s, t, f) =>
			{
				var ret = defaults.GetType().GetField(f).GetValue(defaults);
				Console.WriteLine("FieldLoader: Cannot parse `{0}` into `{2}:{1}`; substituting default `{3}`".F(s, t.Name, f, ret));
				return ret;
			};

			FieldLoader.Load(section, yaml);
		}
	}
}
