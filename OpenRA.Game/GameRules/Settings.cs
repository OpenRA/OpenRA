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
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Server;

namespace OpenRA.GameRules
{
	public enum MouseScrollType { Disabled, Standard, Inverted }
	public enum SoundCashTicks { Disabled, Normal, Extreme }

	public class ServerSettings
	{
		public string Name = "OpenRA Game";
		public int ListenPort = 1234;
		public int ExternalPort = 1234;
		public bool AdvertiseOnline = true;
		public string MasterServer = "http://master.open-ra.org/";
		public bool AllowUPnP = true;
		public bool AllowCheats = false;
		public string Map = null;
		public string[] Ban = null;
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
			MasterServer = other.MasterServer;
			AllowUPnP = other.AllowUPnP;
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
		public float LongTickThreshold = 0.001f;
		public bool SanityCheckUnsyncedCode = false;
		public int Samples = 25;
		public bool IgnoreVersionMismatch = false;
	}

	public class GraphicSettings
	{
		public string Renderer = "Gl";
		public WindowMode Mode = WindowMode.PseudoFullscreen;
		public int2 FullscreenSize = new int2(0,0);
		public int2 WindowedSize = new int2(1024, 768);
		public bool PixelDouble = false;
		public bool CapFramerate = false;
		public int MaxFramerate = 60;

		public int BatchSize = 8192;
		public int NumTempBuffers = 8;
		public int SheetSize = 2048;
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
		
		public SoundCashTicks SoundCashTickType = SoundCashTicks.Extreme;
	}

	public class PlayerSettings
	{
		public string Name = "Newbie";
		public ColorRamp ColorRamp = new ColorRamp(75, 255, 180, 25);
		public string LastServer = "localhost:1234";
	}

	public class GameSettings
	{
		public string[] Mods = { "ra" };

		public bool TeamChatToggle = false;
		public bool ShowShellmap = true;

		public bool ViewportEdgeScroll = true;
		public MouseScrollType MouseScroll = MouseScrollType.Standard;
		public float ViewportEdgeScrollStep = 10f;

		public bool UseClassicMouseStyle = false;

		// Internal game settings
		public int Timestep = 40;

		public string ConnectTo = "";
		public bool AllowDownloading = true;
		public string MapRepository = "http://content.open-ra.org/map/";
	}

	public class KeySettings
	{
		public string PauseKey = "f3";

		public string CycleBaseKey = "backspace";
		public string ToLastEventKey = "space";
		public string ToSelectionKey = "home";
		public string SellKey = "v";
		public string PowerDownKey = "b";
		public string RepairKey = "n";

		public string AttackMoveKey = "a";
		public string StopKey = "s";
		public string ScatterKey = "x";
		public string StanceCycleKey = "z";
		public string DeployKey = "f";

		public string CycleTabsKey = "tab";
	}


	public class Settings
	{
		string SettingsFile;

		public PlayerSettings Player = new PlayerSettings();
		public GameSettings Game = new GameSettings();
		public SoundSettings Sound = new SoundSettings();
		public GraphicSettings Graphics = new GraphicSettings();
		public ServerSettings Server = new ServerSettings();
		public DebugSettings Debug = new DebugSettings();
		public KeySettings Keys = new KeySettings();

		public Dictionary<string, object> Sections;

		public Settings(string file, Arguments args)
		{
			SettingsFile = file;
			Sections = new Dictionary<string, object>()
			{
				{"Player", Player},
				{"Game", Game},
				{"Sound", Sound},
				{"Graphics", Graphics},
				{"Server", Server},
				{"Debug", Debug},
				{"Keys", Keys},
			};

			// Override fieldloader to ignore invalid entries
			var err1 = FieldLoader.UnknownFieldAction;
			var err2 = FieldLoader.InvalidValueAction;

			FieldLoader.UnknownFieldAction = (s,f) =>
			{
				Console.WriteLine( "Ignoring unknown field `{0}` on `{1}`".F( s, f.Name ) );
			};

			if (File.Exists(SettingsFile))
			{
				var yaml = MiniYaml.DictFromFile(SettingsFile);

				foreach (var kv in Sections)
					if (yaml.ContainsKey(kv.Key))
						LoadSectionYaml(yaml[kv.Key], kv.Value);
			}

			// Override with commandline args
			foreach (var kv in Sections)
				foreach (var f in kv.Value.GetType().GetFields())
					if (args.Contains(kv.Key+"."+f.Name))
						FieldLoader.LoadField( kv.Value, f.Name, args.GetValue(kv.Key+"."+f.Name, "") );

			FieldLoader.UnknownFieldAction = err1;
			FieldLoader.InvalidValueAction = err2;
		}

		public void Save()
		{
			var root = new List<MiniYamlNode>();
			foreach( var kv in Sections )
				root.Add( new MiniYamlNode( kv.Key, FieldSaver.SaveDifferences(kv.Value, Activator.CreateInstance(kv.Value.GetType())) ) );

			root.WriteToFile(SettingsFile);
		}

		void LoadSectionYaml(MiniYaml yaml, object section)
		{
			var defaults = Activator.CreateInstance(section.GetType());
			FieldLoader.InvalidValueAction = (s,t,f) =>
			{
				var ret = defaults.GetType().GetField(f).GetValue(defaults);
				Console.WriteLine("FieldLoader: Cannot parse `{0}` into `{2}:{1}`; substituting default `{3}`".F(s,t.Name,f,ret) );
				return ret;
			};

			FieldLoader.Load(section, yaml);
		}
	}
}
