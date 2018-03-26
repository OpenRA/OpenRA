#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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

		[Desc("Reports the game to the master server list.")]
		public bool AdvertiseOnline = true;

		[Desc("Locks the game with a password.")]
		public string Password = "";

		[Desc("Allow users to enable NAT discovery for external IP detection and automatic port forwarding.")]
		public bool DiscoverNatDevices = false;

		[Desc("Time in milliseconds to search for UPnP enabled NAT devices.")]
		public int NatDiscoveryTimeout = 1000;

		[Desc("Starts the game with a default map. Input as hash that can be obtained by the utility.")]
		public string Map = null;

		[Desc("Takes a comma separated list of IP addresses that are not allowed to join.")]
		public string[] Ban = { };

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

		[Desc("Amount of time required for triggering perf.log output.")]
		public float LongTickThresholdMs = 1;

		public bool SanityCheckUnsyncedCode = false;
		public int Samples = 25;

		[Desc("Show incompatible games in server browser.")]
		public bool IgnoreVersionMismatch = false;

		public bool StrictActivityChecking = false;

		[Desc("Check whether a newer version is available online.")]
		public bool CheckVersion = true;

		[Desc("Allow the collection of anonymous data such as Operating System, .NET runtime, OpenGL version and language settings.")]
		public bool SendSystemInformation = true;

		public int SystemInformationVersionPrompt = 0;
		public string UUID = System.Guid.NewGuid().ToString();
		public bool EnableDebugCommandsInReplays = false;
	}

	public class GraphicSettings
	{
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

		[Desc("Disable high resolution DPI scaling on Windows operating systems.")]
		public bool DisableWindowsDPIScaling = true;

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

		public string Device = null;

		public bool CashTicks = true;
		public bool Mute = false;
	}

	public class PlayerSettings
	{
		[Desc("Sets the player nickname for in-game and IRC chat.")]
		public string Name = "Newbie";
		public HSLColor Color = new HSLColor(75, 255, 180);
		public string LastServer = "localhost:1234";
		public HSLColor[] CustomColors = { };
	}

	public class GameSettings
	{
		public string Platform = "Default";

		public bool ViewportEdgeScroll = true;
		public int ViewportEdgeScrollMargin = 5;

		public bool LockMouseWindow = false;
		public MouseScrollType MiddleMouseScroll = MouseScrollType.Standard;
		public MouseScrollType RightMouseScroll = MouseScrollType.Disabled;
		public MouseButtonPreference MouseButtonPreference = new MouseButtonPreference();
		public float ViewportEdgeScrollStep = 10f;
		public float UIScrollSpeed = 50f;
		public int SelectionDeadzone = 24;
		public int MouseScrollDeadzone = 8;

		public bool UseClassicMouseStyle = false;
		public StatusBarsType StatusBars = StatusBarsType.Standard;
		public bool UsePlayerStanceColors = false;
		public bool DrawTargetLine = true;

		public bool AllowDownloading = true;

		public bool AllowZoom = true;
		public Modifiers ZoomModifier = Modifiers.Ctrl;

		public bool FetchNews = true;

		public MPGameFilters MPGameFilters = MPGameFilters.Waiting | MPGameFilters.Empty | MPGameFilters.Protected | MPGameFilters.Started;
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
					yamlCache = MiniYaml.FromFile(settingsFile);
					foreach (var yamlSection in yamlCache)
					{
						object settingsSection;
						if (Sections.TryGetValue(yamlSection.Key, out settingsSection))
							LoadSectionYaml(yamlSection.Value, settingsSection);
					}

					var keysNode = yamlCache.FirstOrDefault(n => n.Key == "Keys");
					if (keysNode != null)
						foreach (var node in keysNode.Value.Nodes)
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
