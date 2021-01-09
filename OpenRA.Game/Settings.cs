#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	public enum MouseScrollType { Disabled, Standard, Inverted, Joystick }
	public enum StatusBarsType { Standard, DamageShow, AlwaysShow }
	public enum TargetLinesType { Disabled, Manual, Automatic }

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

	public enum WorldViewport { Native, Close, Medium, Far }

	public class ServerSettings
	{
		[Desc("Sets the server name.")]
		public string Name = "OpenRA Game";

		[Desc("Sets the internal port.")]
		public int ListenPort = 1234;

		[Desc("Reports the game to the master server list.")]
		public bool AdvertiseOnline = true;

		[Desc("Locks the game with a password.")]
		public string Password = "";

		[Desc("Allow users to enable NAT discovery for external IP detection and automatic port forwarding.")]
		public bool DiscoverNatDevices = false;

		[Desc("Time in milliseconds to search for UPnP enabled NAT devices.")]
		public int NatDiscoveryTimeout = 5000;

		[Desc("Starts the game with a default map. Input as hash that can be obtained by the utility.")]
		public string Map = null;

		[Desc("Takes a comma separated list of IP addresses that are not allowed to join.")]
		public string[] Ban = { };

		[Desc("For dedicated servers only, allow anonymous clients to join.")]
		public bool RequireAuthentication = false;

		[Desc("For dedicated servers only, if non-empty, only allow authenticated players with these profile IDs to join.")]
		public int[] ProfileIDWhitelist = { };

		[Desc("For dedicated servers only, if non-empty, always reject players with these user IDs from joining.")]
		public int[] ProfileIDBlacklist = { };

		[Desc("For dedicated servers only, controls whether a game can be started with just one human player in the lobby.")]
		public bool EnableSingleplayer = false;

		[Desc("Query map information from the Resource Center if they are not available locally.")]
		public bool QueryMapRepository = true;

		[Desc("Enable client-side report generation to help debug desync errors.")]
		public bool EnableSyncReports = false;

		[Desc("Use the new server-sync netcode (experimental).")]
		public bool UseNewNetcode = false;

		[Desc("Sets the timestamp format. Defaults to the ISO 8601 standard.")]
		public string TimestampFormat = "yyyy-MM-ddTHH:mm:ss";

		[Desc("Allow clients to see anonymised IPs for other clients.")]
		public bool ShareAnonymizedIPs = true;

		[Desc("Allow clients to see the country of other clients.")]
		public bool EnableGeoIP = true;

		[Desc("For dedicated servers only, save replays for all games played.")]
		public bool RecordReplays = false;

		public ServerSettings Clone()
		{
			return (ServerSettings)MemberwiseClone();
		}
	}

	public class DebugSettings
	{
		[Desc("Display average FPS and tick/render times")]
		public bool PerfText = false;

		[Desc("Display a graph with various profiling traces")]
		public bool PerfGraph = false;

		[Desc("Numer of samples to average over when calculating tick and render times.")]
		public int Samples = 25;

		[Desc("Check whether a newer version is available online.")]
		public bool CheckVersion = true;

		[Desc("Allow the collection of anonymous data such as Operating System, .NET runtime, OpenGL version and language settings.")]
		public bool SendSystemInformation = true;

		[Desc("Version of sysinfo that the player last opted in or out of.")]
		public int SystemInformationVersionPrompt = 0;

		[Desc("Sysinfo anonymous user identifier.")]
		public string UUID = Guid.NewGuid().ToString();

		[Desc("Enable hidden developer settings in the Advanced settings tab.")]
		public bool DisplayDeveloperSettings = false;

		[Desc("Display bot debug messages in the game chat.")]
		public bool BotDebug = false;

		[Desc("Display Lua debug messages in the game chat.")]
		public bool LuaDebug = false;

		[Desc("Enable the chat field during replays to allow use of console commands.")]
		public bool EnableDebugCommandsInReplays = false;

		[Desc("Amount of time required for triggering perf.log output.")]
		public float LongTickThresholdMs = 1;

		[Desc("Throw an exception if the world sync hash changes while evaluating user input.")]
		public bool SyncCheckUnsyncedCode = false;

		[Desc("Throw an exception if the world sync hash changes while evaluating BotModules.")]
		public bool SyncCheckBotModuleCode = false;
	}

	public class GraphicSettings
	{
		[Desc("This can be set to Windowed, Fullscreen or PseudoFullscreen.")]
		public WindowMode Mode = WindowMode.PseudoFullscreen;

		[Desc("Enable VSync.")]
		public bool VSync = true;

		[Desc("Screen resolution in fullscreen mode.")]
		public int2 FullscreenSize = new int2(0, 0);

		[Desc("Screen resolution in windowed mode.")]
		public int2 WindowedSize = new int2(1024, 768);

		public bool CursorDouble = false;
		public WorldViewport ViewportDistance = WorldViewport.Medium;
		public float UIScale = 1;

		[Desc("Add a frame rate limiter.")]
		public bool CapFramerate = false;

		[Desc("At which frames per second to cap the framerate.")]
		public int MaxFramerate = 60;

		[Desc("Disable separate OpenGL render thread on Windows operating systems.")]
		public bool DisableWindowsRenderThread = true;

		[Desc("Disable the OpenGL debug message callback feature.")]
		public bool DisableGLDebugMessageCallback = false;

		[Desc("Disable operating-system provided cursor rendering.")]
		public bool DisableHardwareCursors = false;

		[Desc("Disable legacy OpenGL 2.1 support.")]
		public bool DisableLegacyGL = true;

		[Desc("Display index to use in a multi-monitor fullscreen setup.")]
		public int VideoDisplay = 0;

		[Desc("Preferred OpenGL profile to use.",
			"Modern: OpenGL Core Profile 3.2 or greater.",
			"Embedded: OpenGL ES 3.0 or greater.",
			"Legacy: OpenGL 2.1 with framebuffer_object extension (requires DisableLegacyGL: False)",
			"Automatic: Use the first supported profile.")]
		public GLProfile GLProfile = GLProfile.Automatic;

		public int BatchSize = 8192;
		public int SheetSize = 2048;
	}

	public class SoundSettings
	{
		public float SoundVolume = 0.5f;
		public float MusicVolume = 0.5f;
		public float VideoVolume = 0.5f;

		public bool Shuffle = false;
		public bool Repeat = false;

		public string Device = null;

		public bool CashTicks = true;
		public bool Mute = false;
		public bool MuteBackgroundMusic = false;
	}

	public class PlayerSettings
	{
		[Desc("Sets the player nickname.")]
		public string Name = "Commander";
		public Color Color = Color.FromArgb(200, 32, 32);
		public string LastServer = "localhost:1234";
		public Color[] CustomColors = { };
	}

	public class GameSettings
	{
		public string Platform = "Default";

		public bool ViewportEdgeScroll = true;
		public int ViewportEdgeScrollMargin = 5;

		public bool LockMouseWindow = false;
		public MouseScrollType MouseScroll = MouseScrollType.Joystick;
		public MouseButtonPreference MouseButtonPreference = new MouseButtonPreference();
		public float ViewportEdgeScrollStep = 30f;
		public float UIScrollSpeed = 50f;
		public float ZoomSpeed = 0.04f;
		public int SelectionDeadzone = 24;
		public int MouseScrollDeadzone = 8;

		public bool UseClassicMouseStyle = false;
		public bool UseAlternateScrollButton = false;

		public StatusBarsType StatusBars = StatusBarsType.Standard;
		public TargetLinesType TargetLines = TargetLinesType.Manual;
		public bool UsePlayerStanceColors = false;

		public bool AllowDownloading = true;

		[Desc("Filename of the authentication profile to use.")]
		public string AuthProfile = "player.oraid";

		public Modifiers ZoomModifier = Modifiers.None;

		public bool FetchNews = true;

		[Desc("Version of introduction prompt that the player last viewed.")]
		public int IntroductionPromptVersion = 0;

		public MPGameFilters MPGameFilters = MPGameFilters.Waiting | MPGameFilters.Empty | MPGameFilters.Protected | MPGameFilters.Started;

		public bool PauseShellmap = false;
	}

	public class Settings
	{
		readonly string settingsFile;

		public readonly PlayerSettings Player = new PlayerSettings();
		public readonly GameSettings Game = new GameSettings();
		public readonly SoundSettings Sound = new SoundSettings();
		public readonly GraphicSettings Graphics = new GraphicSettings();
		public readonly ServerSettings Server = new ServerSettings();
		public readonly DebugSettings Debug = new DebugSettings();
		internal Dictionary<string, Hotkey> Keys = new Dictionary<string, Hotkey>();

		public readonly Dictionary<string, object> Sections;

		// A direct clone of the file loaded from disk.
		// Any changed settings will be merged over this on save,
		// allowing us to persist any unknown configuration keys
		readonly List<MiniYamlNode> yamlCache = new List<MiniYamlNode>();

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
			};

			// Override fieldloader to ignore invalid entries
			var err1 = FieldLoader.UnknownFieldAction;
			var err2 = FieldLoader.InvalidValueAction;
			try
			{
				FieldLoader.UnknownFieldAction = (s, f) => Console.WriteLine("Ignoring unknown field `{0}` on `{1}`".F(s, f.Name));

				if (File.Exists(settingsFile))
				{
					yamlCache = MiniYaml.FromFile(settingsFile, false);
					foreach (var yamlSection in yamlCache)
					{
						if (yamlSection.Key != null && Sections.TryGetValue(yamlSection.Key, out var settingsSection))
							LoadSectionYaml(yamlSection.Value, settingsSection);
					}

					var keysNode = yamlCache.FirstOrDefault(n => n.Key == "Keys");
					if (keysNode != null)
						foreach (var node in keysNode.Value.Nodes)
							if (node.Key != null)
								Keys[node.Key] = FieldLoader.GetValue<Hotkey>(node.Key, node.Value.Value);
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
			foreach (var kv in Sections)
			{
				var sectionYaml = yamlCache.FirstOrDefault(x => x.Key == kv.Key);
				if (sectionYaml == null)
				{
					sectionYaml = new MiniYamlNode(kv.Key, new MiniYaml(""));
					yamlCache.Add(sectionYaml);
				}

				var defaultValues = Activator.CreateInstance(kv.Value.GetType());
				var fields = FieldLoader.GetTypeLoadInfo(kv.Value.GetType());
				foreach (var fli in fields)
				{
					var serialized = FieldSaver.FormatValue(kv.Value, fli.Field);
					var defaultSerialized = FieldSaver.FormatValue(defaultValues, fli.Field);

					// Fields with their default value are not saved in the settings yaml
					// Make sure that we erase any previously defined custom values
					if (serialized == defaultSerialized)
						sectionYaml.Value.Nodes.RemoveAll(n => n.Key == fli.YamlName);
					else
					{
						// Update or add the custom value
						var fieldYaml = sectionYaml.Value.Nodes.FirstOrDefault(n => n.Key == fli.YamlName);
						if (fieldYaml != null)
							fieldYaml.Value.Value = serialized;
						else
							sectionYaml.Value.Nodes.Add(new MiniYamlNode(fli.YamlName, new MiniYaml(serialized)));
					}
				}
			}

			var keysYaml = yamlCache.FirstOrDefault(x => x.Key == "Keys");
			if (keysYaml == null)
			{
				keysYaml = new MiniYamlNode("Keys", new MiniYaml(""));
				yamlCache.Add(keysYaml);
			}

			keysYaml.Value.Nodes.Clear();
			foreach (var kv in Keys)
				keysYaml.Value.Nodes.Add(new MiniYamlNode(kv.Key, FieldSaver.FormatValue(kv.Value)));

			yamlCache.WriteToFile(settingsFile);
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
