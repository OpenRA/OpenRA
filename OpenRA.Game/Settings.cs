#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA
{
	public enum MouseScrollType { Disabled, Standard, Inverted, Joystick }
	public enum StatusBarsType { Standard, DamageShow, AlwaysShow }

	[Flags]
	public enum MPGameFilters
	{
		None = 0,
		Waiting = 1,
		Empty = 2,
		Protected = 4,
		Started = 8,
		Incompatible = 16
	}

	public class ServerSettings
	{
		[Desc("Sets the server name.")]
		public string Name = "OpenRA Game";

		[Desc("Sets the internal port.")]
		public int ListenPort = 1234;

		[Desc("Sets the port advertised to the master server.")]
		public int ExternalPort = 1234;

		[Desc("Reports the game to the master server list.")]
		public bool AdvertiseOnline = true;

		[Desc("Locks the game with a password.")]
		public string Password = "";

		public string MasterServer = "http://master.openra.net/";

		[Desc("Allow users to enable NAT discovery for external IP detection and automatic port forwarding.")]
		public bool DiscoverNatDevices = false;

		[Desc("Set this to false to disable UPnP even if compatible devices are found.")]
		public bool AllowPortForward = true;

		[Desc("Time in milliseconds to search for UPnP enabled NAT devices.")]
		public int NatDiscoveryTimeout = 1000;

		[Desc("Starts the game with a default map. Input as hash that can be obtained by the utility.")]
		public string Map = null;

		[Desc("Takes a comma separated list of IP addresses that are not allowed to join.")]
		public string[] Ban = { };

		[Desc("Value in milliseconds when to terminate the game. Needs to be at least 10000 (10 s) to enable the timer.")]
		public int TimeOut = 0;

		[Desc("For dedicated servers only, controls whether a game can be started with just one human player in the lobby.")]
		public bool EnableSingleplayer = false;

		[Desc("Query map information from the Resource Center if they are not available locally.")]
		public bool QueryMapRepository = true;

		public string TimestampFormat = "s";

		public ServerSettings Clone()
		{
			return (ServerSettings)MemberwiseClone();
		}
	}

	public class DebugSettings
	{
		public bool BotDebug = false;
		public bool LuaDebug = false;
		public bool PerfText = false;
		public bool PerfGraph = false;
		public float LongTickThresholdMs = 1;
		public bool SanityCheckUnsyncedCode = false;
		public int Samples = 25;
		public bool IgnoreVersionMismatch = false;
		public bool SendSystemInformation = true;
		public int SystemInformationVersionPrompt = 0;
		public string UUID = System.Guid.NewGuid().ToString();
	}

	public class GraphicSettings
	{
		public string Renderer = "Default";

		[Desc("This can be set to Windowed, Fullscreen or PseudoFullscreen.")]
		public WindowMode Mode = WindowMode.PseudoFullscreen;

		[Desc("Screen resolution in fullscreen mode.")]
		public int2 FullscreenSize = new int2(0, 0);

		[Desc("Screen resolution in windowed mode.")]
		public int2 WindowedSize = new int2(1024, 768);

		public bool HardwareCursors = true;

		public bool PixelDouble = false;
		public bool CursorDouble = false;

		[Desc("Add a frame rate limiter. It is recommended to not disable this.")]
		public bool CapFramerate = true;

		[Desc("At which frames per second to cap the framerate.")]
		public int MaxFramerate = 60;

		public int BatchSize = 8192;
		public int SheetSize = 2048;

		public string Language = "english";
		public string DefaultLanguage = "english";

		public ImageFormat ScreenshotFormat = ImageFormat.Png;
	}

	public class SoundSettings
	{
		public float SoundVolume = 0.5f;
		public float MusicVolume = 0.5f;
		public float VideoVolume = 0.5f;

		public bool Shuffle = false;
		public bool Repeat = false;

		public string Engine = "Default";
		public string Device = null;

		public bool CashTicks = true;
		public bool Mute = false;
	}

	public class PlayerSettings
	{
		public string Name = "Newbie";
		public HSLColor Color = new HSLColor(75, 255, 180);
		public string LastServer = "localhost:1234";
	}

	public class GameSettings
	{
		[Desc("Load a specific mod on startup. Shipped ones include: ra, cnc and d2k")]
		public string Mod = "modchooser";
		public string PreviousMod = "ra";

		public bool ShowShellmap = true;

		public bool ViewportEdgeScroll = true;
		public bool LockMouseWindow = false;
		public MouseScrollType MiddleMouseScroll = MouseScrollType.Standard;
		public MouseScrollType RightMouseScroll = MouseScrollType.Disabled;
		public MouseButtonPreference MouseButtonPreference = new MouseButtonPreference();
		public float ViewportEdgeScrollStep = 10f;
		public float UIScrollSpeed = 50f;
		public int SelectionDeadzone = 24;
		public int JoystickScrollDeadzone = 8;

		public bool UseClassicMouseStyle = false;
		public StatusBarsType StatusBars = StatusBarsType.Standard;
		public bool UsePlayerStanceColors = false;
		public bool DrawTargetLine = true;

		public bool AllowDownloading = true;
		public string MapRepository = "http://resource.openra.net/map/";

		public bool AllowZoom = true;
		public Modifiers ZoomModifier = Modifiers.Ctrl;

		public bool FetchNews = true;
		public string NewsUrl = "http://master.openra.net/gamenews";

		public MPGameFilters MPGameFilters = MPGameFilters.Waiting | MPGameFilters.Empty | MPGameFilters.Protected | MPGameFilters.Started;
	}

	public class KeySettings
	{
		public Hotkey CycleBaseKey = new Hotkey(Keycode.H, Modifiers.None);
		public Hotkey ToLastEventKey = new Hotkey(Keycode.SPACE, Modifiers.None);
		public Hotkey ToSelectionKey = new Hotkey(Keycode.HOME, Modifiers.None);
		public Hotkey SelectAllUnitsKey = new Hotkey(Keycode.Q, Modifiers.None);
		public Hotkey SelectUnitsByTypeKey = new Hotkey(Keycode.W, Modifiers.None);

		public Hotkey MapScrollUp = new Hotkey(Keycode.UP, Modifiers.None);
		public Hotkey MapScrollDown = new Hotkey(Keycode.DOWN, Modifiers.None);
		public Hotkey MapScrollLeft = new Hotkey(Keycode.LEFT, Modifiers.None);
		public Hotkey MapScrollRight = new Hotkey(Keycode.RIGHT, Modifiers.None);

		public Hotkey MapPushTop = new Hotkey(Keycode.UP, Modifiers.Alt);
		public Hotkey MapPushBottom = new Hotkey(Keycode.DOWN, Modifiers.Alt);
		public Hotkey MapPushLeftEdge = new Hotkey(Keycode.LEFT, Modifiers.Alt);
		public Hotkey MapPushRightEdge = new Hotkey(Keycode.RIGHT, Modifiers.Alt);

		public Hotkey ViewPortBookmarkSaveSlot1 = new Hotkey(Keycode.Q, Modifiers.Ctrl);
		public Hotkey ViewPortBookmarkSaveSlot2 = new Hotkey(Keycode.W, Modifiers.Ctrl);
		public Hotkey ViewPortBookmarkSaveSlot3 = new Hotkey(Keycode.E, Modifiers.Ctrl);
		public Hotkey ViewPortBookmarkSaveSlot4 = new Hotkey(Keycode.R, Modifiers.Ctrl);

		public Hotkey ViewPortBookmarkUseSlot1 = new Hotkey(Keycode.Q, Modifiers.Alt);
		public Hotkey ViewPortBookmarkUseSlot2 = new Hotkey(Keycode.W, Modifiers.Alt);
		public Hotkey ViewPortBookmarkUseSlot3 = new Hotkey(Keycode.E, Modifiers.Alt);
		public Hotkey ViewPortBookmarkUseSlot4 = new Hotkey(Keycode.R, Modifiers.Alt);

		public Hotkey PauseKey = new Hotkey(Keycode.PAUSE, Modifiers.None);
		public Hotkey PlaceBeaconKey = new Hotkey(Keycode.B, Modifiers.None);
		public Hotkey SellKey = new Hotkey(Keycode.Z, Modifiers.None);
		public Hotkey PowerDownKey = new Hotkey(Keycode.X, Modifiers.None);
		public Hotkey RepairKey = new Hotkey(Keycode.C, Modifiers.None);

		public Hotkey NextProductionTabKey = new Hotkey(Keycode.PAGEDOWN, Modifiers.None);
		public Hotkey PreviousProductionTabKey = new Hotkey(Keycode.PAGEUP, Modifiers.None);
		public Hotkey CycleProductionBuildingsKey = new Hotkey(Keycode.TAB, Modifiers.None);

		public Hotkey AttackMoveKey = new Hotkey(Keycode.A, Modifiers.None);
		public Hotkey StopKey = new Hotkey(Keycode.S, Modifiers.None);
		public Hotkey ScatterKey = new Hotkey(Keycode.X, Modifiers.Ctrl);
		public Hotkey DeployKey = new Hotkey(Keycode.F, Modifiers.None);
		public Hotkey StanceCycleKey = new Hotkey(Keycode.Z, Modifiers.Ctrl);
		public Hotkey GuardKey = new Hotkey(Keycode.D, Modifiers.None);

		public Hotkey ObserverCombinedView = new Hotkey(Keycode.MINUS, Modifiers.None);
		public Hotkey ObserverWorldView = new Hotkey(Keycode.EQUALS, Modifiers.None);

		public Hotkey CycleStatusBarsKey = new Hotkey(Keycode.COMMA, Modifiers.None);
		public Hotkey TogglePixelDoubleKey = new Hotkey(Keycode.PERIOD, Modifiers.None);
		public Hotkey TogglePlayerStanceColorsKey = new Hotkey(Keycode.COMMA, Modifiers.Ctrl);

		public Hotkey DevReloadChromeKey = new Hotkey(Keycode.C, Modifiers.Ctrl | Modifiers.Shift);
		public Hotkey HideUserInterfaceKey = new Hotkey(Keycode.H, Modifiers.Ctrl | Modifiers.Shift);
		public Hotkey TakeScreenshotKey = new Hotkey(Keycode.P, Modifiers.Ctrl);
		public Hotkey ToggleMuteKey = new Hotkey(Keycode.M, Modifiers.None);

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

		public Hotkey ProductionTypeBuildingKey = new Hotkey(Keycode.E, Modifiers.None);
		public Hotkey ProductionTypeDefenseKey = new Hotkey(Keycode.R, Modifiers.None);
		public Hotkey ProductionTypeInfantryKey = new Hotkey(Keycode.T, Modifiers.None);
		public Hotkey ProductionTypeVehicleKey = new Hotkey(Keycode.Y, Modifiers.None);
		public Hotkey ProductionTypeAircraftKey = new Hotkey(Keycode.U, Modifiers.None);
		public Hotkey ProductionTypeNavalKey = new Hotkey(Keycode.I, Modifiers.None);
		public Hotkey ProductionTypeTankKey = new Hotkey(Keycode.I, Modifiers.None);
		public Hotkey ProductionTypeMerchantKey = new Hotkey(Keycode.O, Modifiers.None);
		public Hotkey ProductionTypeUpgradeKey = new Hotkey(Keycode.R, Modifiers.None);

		public Hotkey SupportPower01Key = new Hotkey(Keycode.UNKNOWN, Modifiers.None);
		public Hotkey SupportPower02Key = new Hotkey(Keycode.UNKNOWN, Modifiers.None);
		public Hotkey SupportPower03Key = new Hotkey(Keycode.UNKNOWN, Modifiers.None);
		public Hotkey SupportPower04Key = new Hotkey(Keycode.UNKNOWN, Modifiers.None);
		public Hotkey SupportPower05Key = new Hotkey(Keycode.UNKNOWN, Modifiers.None);
		public Hotkey SupportPower06Key = new Hotkey(Keycode.UNKNOWN, Modifiers.None);

		public Hotkey ReplaySpeedSlowKey = new Hotkey(Keycode.F5, Modifiers.None);
		public Hotkey ReplaySpeedRegularKey = new Hotkey(Keycode.F6, Modifiers.None);
		public Hotkey ReplaySpeedFastKey = new Hotkey(Keycode.F7, Modifiers.None);
		public Hotkey ReplaySpeedMaxKey = new Hotkey(Keycode.F8, Modifiers.None);

		public Hotkey NextTrack = new Hotkey(Keycode.AUDIONEXT, Modifiers.None);
		public Hotkey PreviousTrack = new Hotkey(Keycode.AUDIOPREV, Modifiers.None);
		public Hotkey StopMusic = new Hotkey(Keycode.AUDIOSTOP, Modifiers.None);
		public Hotkey PauseMusic = new Hotkey(Keycode.AUDIOPLAY, Modifiers.None);

		static readonly Func<KeySettings, Hotkey>[] ProductionKeys = GetKeys(24, "Production");
		static readonly Func<KeySettings, Hotkey>[] SupportPowerKeys = GetKeys(6, "SupportPower");

		static Func<KeySettings, Hotkey>[] GetKeys(int count, string prefix)
		{
			var keySettings = Expression.Parameter(typeof(KeySettings), "keySettings");
			return Exts.MakeArray(count, i => Expression.Lambda<Func<KeySettings, Hotkey>>(
				Expression.Field(keySettings, "{0}{1:D2}Key".F(prefix, i + 1)), keySettings).Compile());
		}

		public Hotkey GetProductionHotkey(int index)
		{
			return GetKey(ProductionKeys, index);
		}

		public Hotkey GetSupportPowerHotkey(int index)
		{
			return GetKey(SupportPowerKeys, index);
		}

		Hotkey GetKey(Func<KeySettings, Hotkey>[] keys, int index)
		{
			if (index < 0 || index >= keys.Length)
				return Hotkey.Invalid;

			return keys[index](this);
		}
	}

	public class ChatSettings
	{
		public string Hostname = "irc.openra.net";
		public int Port = 6667;
		public string Channel = "lobby";
		public string Nickname = "Newbie";
		public string QuitMessage = "Battle control terminated!";
		public string TimestampFormat = "HH:mm";
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
		public ChatSettings Chat = new ChatSettings();

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
				{ "Chat", Chat }
			};

			// Override fieldloader to ignore invalid entries
			var err1 = FieldLoader.UnknownFieldAction;
			var err2 = FieldLoader.InvalidValueAction;
			try
			{
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
			}
			finally
			{
				FieldLoader.UnknownFieldAction = err1;
				FieldLoader.InvalidValueAction = err2;
			}
		}

		public void Save()
		{
			var root = new List<MiniYamlNode>();
			foreach (var kv in Sections)
				root.Add(new MiniYamlNode(kv.Key, FieldSaver.SaveDifferences(kv.Value, Activator.CreateInstance(kv.Value.GetType()))));

			root.WriteToFile(settingsFile);
		}

		static string SanitizedName(string dirty)
		{
			if (string.IsNullOrEmpty(dirty))
				return null;

			var clean = dirty;

			// reserved characters for MiniYAML and JSON
			var disallowedChars = new char[] { '#', '@', ':', '\n', '\t', '[', ']', '{', '}', '"', '`' };
			foreach (var disallowedChar in disallowedChars)
				clean = clean.Replace(disallowedChar.ToString(), string.Empty);

			return clean;
		}

		public static string SanitizedServerName(string dirty)
		{
			var clean = SanitizedName(dirty);
			if (string.IsNullOrWhiteSpace(clean))
				return new ServerSettings().Name;
			else
				return clean;
		}

		public static string SanitizedPlayerName(string dirty)
		{
			var forbiddenNames = new string[] { "Open", "Closed" };
			var botNames = OpenRA.Game.ModData.DefaultRules.Actors["player"].TraitInfos<IBotInfo>().Select(t => t.Name);

			var clean = SanitizedName(dirty);

			if (string.IsNullOrWhiteSpace(clean) || forbiddenNames.Contains(clean) || botNames.Contains(clean))
				clean = new PlayerSettings().Name;

			// avoid UI glitches
			if (clean.Length > 16)
				clean = clean.Substring(0, 16);

			return clean;
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
