#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA.Primitives;

namespace OpenRA
{
	public enum MouseControlStyle { Classic, Modern, OtherRTS }
	public enum MouseScrollType { Disabled, Standard, Inverted, Joystick }
	public enum StatusBarsType { Standard, DamageShow, AlwaysShow }
	public enum TargetLinesType { Disabled, Manual, Automatic }

	public enum MouseActionType { Contextual, ConfirmOrder, GlobalCommand, SupportPower, PlaceBuilding }

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

	[Flags]
	public enum TextNotificationPoolFilters
	{
		None = 0,
		Feedback = 1,
		Transients = 2
	}

	public enum WorldViewport { Native, Close, Medium, Far }

	public abstract class SettingsModule
	{
		[AttributeUsage(AttributeTargets.Class)]
		public sealed class YamlNodeAttribute(string key, bool shared = true) : Attribute
		{
			public readonly string Key = key;
			public readonly bool Shared = shared;
		}

		[FieldLoader.Ignore]
		internal string ModInstance;

		[FieldLoader.Ignore]
		public readonly MiniYamlBuilder Yaml;

		internal void Commit()
		{
			var defaultValues = Activator.CreateInstance(GetType());
			var fields = FieldLoader.GetTypeLoadInfo(GetType());
			foreach (var fli in fields)
			{
				var serialized = FieldSaver.FormatValue(this, fli.Field);
				var defaultSerialized = FieldSaver.FormatValue(defaultValues, fli.Field);

				// Fields with their default value are not saved in the settings yaml
				// Make sure that we erase any previously defined custom values
				Yaml.Nodes.RemoveAll(n => n.Key == fli.YamlName);
				if (serialized != defaultSerialized)
					Yaml.Nodes.Add(new MiniYamlNodeBuilder(fli.YamlName, new MiniYamlBuilder(serialized)));
			}
		}

		public void Save()
		{
			Commit();
			Game.Settings.Save(false);
		}
	}

	[YamlNode("Server", shared: true)]
	public class ServerSettings : SettingsModule
	{
		[Desc("Sets the server name.")]
		public string Name = "";

		[Desc("Sets the internal port.")]
		public int ListenPort = 1234;

		[Desc("Reports the game to the master server list.")]
		public bool AdvertiseOnline = true;

		[Desc("Reports the game on the local area network.")]
		public bool AdvertiseOnLocalNetwork = true;

		[Desc("Locks the game with a password.")]
		public string Password = "";

		[Desc("Allow users to search UPnP/NAT-PMP enabled devices for automatic port forwarding.")]
		public bool DiscoverNatDevices = false;

		[Desc("Time in seconds for UPnP/NAT-PMP mappings to last.")]
		public int NatPortMappingLifetime = 36000;

		[Desc("Starts the game with a default map. Input as hash that can be obtained by the utility.")]
		public string Map = null;

		[Desc("Takes a comma separated list of IP addresses that are not allowed to join.")]
		public FrozenSet<string> Ban = FrozenSet<string>.Empty;

		[Desc("For dedicated servers only, allow anonymous clients to join.")]
		public bool RequireAuthentication = false;

		[Desc("For dedicated servers only, if non-empty, only allow authenticated players with these profile IDs to join.")]
		public FrozenSet<int> ProfileIDWhitelist = FrozenSet<int>.Empty;

		[Desc("For dedicated servers only, if non-empty, always reject players with these user IDs from joining.")]
		public FrozenSet<int> ProfileIDBlacklist = FrozenSet<int>.Empty;

		[Desc("For dedicated servers only, controls whether a game can be started with just one human player in the lobby.")]
		public bool EnableSingleplayer = false;

		[Desc("Controls whether generated maps can be used.")]
		public bool EnableMapGeneration = true;

		[Desc("Query map information from the Resource Center if they are not available locally.")]
		public bool QueryMapRepository = true;

		[Desc("Enable client-side report generation to help debug desync errors.")]
		public bool EnableSyncReports = false;

		[Desc("Sets the timestamp format. Defaults to the ISO 8601 standard.")]
		public string TimestampFormat = "yyyy-MM-ddTHH:mm:ss";

		[Desc("Allow clients to see anonymised IPs for other clients.")]
		public bool ShareAnonymizedIPs = true;

		[Desc("Allow clients to see the country of other clients.")]
		public bool EnableGeoIP = true;

		[Desc("For dedicated servers only, save replays for all games played.")]
		public bool RecordReplays = false;

		[Desc("For dedicated servers only, treat maps that fail the lint checks as invalid.")]
		public bool EnableLintChecks = true;

		[Desc("For dedicated servers only, a comma separated list of map uids that are allowed to be used.")]
		public FrozenSet<string> MapPool = FrozenSet<string>.Empty;

		[Desc("Delay in milliseconds before newly joined players can send chat messages.")]
		public int FloodLimitJoinCooldown = 5000;

		[Desc("Amount of milliseconds player chat messages are tracked for.")]
		public int FloodLimitInterval = 5000;

		[Desc("Amount of chat messages per FloodLimitInterval a players can send before flood is detected.")]
		public int FloodLimitMessageCount = 5;

		[Desc("Delay in milliseconds before players can send chat messages after flood was detected.")]
		public int FloodLimitCooldown = 15000;

		[Desc("Can players vote to kick other players?")]
		public bool EnableVoteKick = true;

		[Desc("After how much time in milliseconds should the vote kick fail after idling?")]
		public int VoteKickTimer = 30000;

		[Desc("If a vote kick was unsuccessful for how long should the player who started the vote not be able to start new votes?")]
		public int VoteKickerCooldown = 120000;

		public ServerSettings Clone()
		{
			return (ServerSettings)MemberwiseClone();
		}
	}

	[YamlNode("Debug", shared: true)]
	public class DebugSettings : SettingsModule
	{
		[Desc("Display average FPS and tick/render times")]
		public bool PerfText = false;

		[Desc("Display a graph with various profiling traces")]
		public bool PerfGraph = false;

		[Desc("Number of samples to average over when calculating tick and render times.")]
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

		[Desc("Enable perf.log output for traits, activities and effects.")]
		public bool EnableSimulationPerfLogging = false;

		[Desc("Amount of time required for triggering perf.log output.")]
		public float LongTickThresholdMs = 1;

		[Desc("Throw an exception if the world sync hash changes while evaluating user input.")]
		public bool SyncCheckUnsyncedCode = false;

		[Desc("Throw an exception if the world sync hash changes while evaluating BotModules.")]
		public bool SyncCheckBotModuleCode = false;
	}

	[YamlNode("Graphics", shared: true)]
	public class GraphicSettings : SettingsModule
	{
		[Desc("This can be set to Windowed, Fullscreen or PseudoFullscreen.")]
		public WindowMode Mode = WindowMode.PseudoFullscreen;

		[Desc("Enable VSync.")]
		public bool VSync = true;

		[Desc("Screen resolution in fullscreen mode.")]
		public int2 FullscreenSize = new(0, 0);

		[Desc("Screen resolution in windowed mode.")]
		public int2 WindowedSize = new(1024, 768);

		public bool CursorDouble = false;
		public WorldViewport ViewportDistance = WorldViewport.Medium;
		public float UIScale = 1;

		[Desc("Add a frame rate limiter.")]
		public bool CapFramerate = false;

		[Desc("At which frames per second to cap the framerate.")]
		public int MaxFramerate = 60;

		[Desc("Set a frame rate limit of 1 render frame per game simulation frame (overrides CapFramerate/MaxFramerate).")]
		public bool CapFramerateToGameFps = false;

		[Desc("Disable the OpenGL debug message callback feature.")]
		public bool DisableGLDebugMessageCallback = false;

		[Desc("Disable operating-system provided cursor rendering.")]
		public bool DisableHardwareCursors = false;

		[Desc("Display index to use in a multi-monitor fullscreen setup.")]
		public int VideoDisplay = 0;

		[Desc("Preferred OpenGL profile to use.",
			"Modern: OpenGL Core Profile 3.2 or greater.",
			"Embedded: OpenGL ES 3.0 or greater.",
			"Legacy: OpenGL 2.1 with framebuffer_object extension (requires DisableLegacyGL: False)",
			"Automatic: Use the first supported profile.")]
		public GLProfile GLProfile = GLProfile.Automatic;

		public GraphicSettings Clone()
		{
			return (GraphicSettings)MemberwiseClone();
		}
	}

	[YamlNode("Sound", shared: true)]
	public class SoundSettings : SettingsModule
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

		public SoundSettings Clone()
		{
			return (SoundSettings)MemberwiseClone();
		}
	}

	[YamlNode("Player", shared: true)]
	public class PlayerSettings : SettingsModule
	{
		[Desc("Sets the player nickname.")]
		public string Name = "Commander";
		public Color Color = Color.FromArgb(200, 32, 32);
		public string LastServer = "localhost:1234";
		public ImmutableArray<Color> CustomColors = [];
	}

	[YamlNode("Game", shared: true)]
	public class GameSettings : SettingsModule
	{
		public string Platform = "Default";

		public bool ViewportEdgeScroll = true;
		public int ViewportEdgeScrollMargin = 5;

		public bool LockMouseWindow = false;
		public MouseControlStyle MouseControlStyle = MouseControlStyle.Modern;
		public MouseScrollType MouseScroll = MouseScrollType.Joystick;
		public float ViewportEdgeScrollStep = 30f;
		public float UIScrollSpeed = 50f;
		public float ZoomSpeed = 0.04f;
		public int SelectionDeadzone = 24;
		public int MouseScrollDeadzone = 8;

		public bool UseAlternateScrollButton = false;

		public bool HideReplayChat = false;

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

		[Desc("Allow mods to enable the Discord service that can interact with a local Discord client.")]
		public bool EnableDiscordService = true;

		public TextNotificationPoolFilters TextNotificationPoolFilters = TextNotificationPoolFilters.Feedback | TextNotificationPoolFilters.Transients;

		public MouseButton ResolveActionButton(MouseActionType actionType)
		{
			switch (actionType)
			{
				case MouseActionType.ConfirmOrder:
					return MouseControlStyle == MouseControlStyle.Modern ? MouseButton.Right : MouseButton.Left;
				case MouseActionType.Contextual:
					return MouseControlStyle == MouseControlStyle.Classic ? MouseButton.Left : MouseButton.Right;
				default: return MouseButton.Left;
			}
		}

		public MouseButton ResolveCancelButton(MouseActionType actionType)
		{
			return ResolveActionButton(actionType) == MouseButton.Left ? MouseButton.Right : MouseButton.Left;
		}
	}

	public class Settings
	{
		readonly string settingsFile;

		public readonly PlayerSettings Player;
		public readonly GameSettings Game;
		public readonly SoundSettings Sound;
		public readonly GraphicSettings Graphics;
		public readonly ServerSettings Server;
		public readonly DebugSettings Debug;

		readonly Arguments args;
		readonly TypeDictionary modules = [];
		readonly List<MiniYamlNodeBuilder> yaml;

		public Settings(string file, Arguments args)
		{
			settingsFile = file;
			this.args = args;

			if (File.Exists(settingsFile))
				yaml = MiniYaml.FromFile(settingsFile, false)
					.Select(n => new MiniYamlNodeBuilder(n))
					.ToList();
			else
				yaml = [];

			// Load the default sections
			Player = GetOrCreate<PlayerSettings>(null);
			Game = GetOrCreate<GameSettings>(null);
			Sound = GetOrCreate<SoundSettings>(null);
			Graphics = GetOrCreate<GraphicSettings>(null);
			Server = GetOrCreate<ServerSettings>(null);
			Debug = GetOrCreate<DebugSettings>(null);
		}

		public T GetOrCreate<T>(ObjectCreator objectCreator, string mod = null) where T : SettingsModule
		{
			var attribute = typeof(T).GetCustomAttribute<SettingsModule.YamlNodeAttribute>();
			if (attribute == null)
				throw new InvalidDataException("Settings modules must define a YamlNode attribute");

			var module = attribute.Shared ? modules.GetOrDefault<T>() :
				modules.WithInterface<T>().SingleOrDefault(m => m.ModInstance == mod);

			// Lazily load/create the module on first use
			if (module == null)
			{
				if (objectCreator != null)
					module = (T)objectCreator.CreateBasic(typeof(T));
				else
					module = Activator.CreateInstance<T>();

				var nodeKey = attribute.Key;
				if (!attribute.Shared)
				{
					module.ModInstance = mod;
					nodeKey = $"{attribute.Key}@{mod}";
				}

				var err1 = FieldLoader.UnknownFieldAction;
				var err2 = FieldLoader.InvalidValueAction;
				try
				{
					FieldLoader.InvalidValueAction = (s, t, f) =>
					{
						var ret = t.GetField(f)?.GetValue(module);
						Console.WriteLine($"FieldLoader: Cannot parse `{s}` into `{f}:{t.Name}`; substituting default `{ret}`");
						return ret;
					};

					var node = yaml.FirstOrDefault(n => n.Key == nodeKey);
					if (node == null)
					{
						node = new MiniYamlNodeBuilder(nodeKey, "");
						yaml.Add(node);
					}

					typeof(T).GetField(nameof(SettingsModule.Yaml))?.SetValue(module, node.Value);
					FieldLoader.Load(module, node.Value.Build());

					// Override with commandline args
					foreach (var f in typeof(T).GetFields())
					{
						var argName = attribute.Key + "." + f.Name;
						if (args.Contains(argName))
							FieldLoader.LoadFieldOrProperty(module, f.Name, args.GetValue(argName, ""));
					}
				}
				finally
				{
					FieldLoader.UnknownFieldAction = err1;
					FieldLoader.InvalidValueAction = err2;
				}

				modules.Add(module);
			}

			return module;
		}

		public void Save(bool commitModules = true)
		{
			if (commitModules)
				foreach (var m in modules)
					((SettingsModule)m).Commit();

			// Filter out modules with no fields and force a newline between each module
			var container = new[] { null, new MiniYamlNodeBuilder("", "") };
			IEnumerable<MiniYamlNodeBuilder> AddSpacer(MiniYamlNodeBuilder n)
			{
				container[0] = n;
				return container;
			}

			yaml.Where(n => n.Value.Nodes.Count > 0).SelectMany(AddSpacer).WriteToFile(settingsFile);
		}

		static string SanitizedName(string dirty)
		{
			if (string.IsNullOrEmpty(dirty))
				return null;

			var clean = dirty;

			// reserved characters for MiniYAML and JSON
			var disallowedChars = new char[] { '#', '@', ':', '\n', '\t', '[', ']', '{', '}', '<', '>', '"', '`' };
			foreach (var disallowedChar in disallowedChars)
				clean = clean.Replace(disallowedChar.ToString(), string.Empty);

			return clean;
		}

		public string SanitizedServerName(string dirty)
		{
			var clean = SanitizedName(dirty);
			if (string.IsNullOrWhiteSpace(clean))
				return $"{SanitizedPlayerName(Player.Name)}'s Game";
			else
				return clean;
		}

		public static string SanitizedPlayerName(string dirty)
		{
			var forbiddenNames = new string[] { "Open", "Closed" };

			var clean = SanitizedName(dirty);

			if (string.IsNullOrWhiteSpace(clean) || forbiddenNames.Contains(clean))
				clean = new PlayerSettings().Name;

			// avoid UI glitches
			if (clean.Length > 16)
				clean = clean[..16];

			return clean;
		}
	}
}
