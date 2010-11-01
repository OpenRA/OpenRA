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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Server;

namespace OpenRA.GameRules
{
	public class ServerSettings
	{
		public string Name = "OpenRA Game";
		public int ListenPort = 1234;
		public int ExternalPort = 1234;
		public bool AdvertiseOnline = true;
		public string MasterServer = "http://master.open-ra.org/";
		public bool AllowCheats = false;
		public string ExtensionDll = "";
		public string ExtensionClass = "";

		/* not storeable */
		public IServerExtension Extension { get; set; }
		public string ExtensionYaml { get; set; }
		public bool IsDedicated { get; set; }
	}
	
	public class DebugSettings
	{
		public bool PerfGraph = false;
		public bool RecordSyncReports = true;
		public float LongTickThreshold = 0.001f;
	}

	public class GraphicSettings
	{
		public WindowMode Mode = WindowMode.PseudoFullscreen;
		public int2 FullscreenSize = new int2(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
		public int2 WindowedSize = new int2(1024, 768);
		public readonly int2 MinResolution = new int2(800, 600);
		public readonly string RenderEngine = "OpenRA.Gl.dll";
		public readonly string NullRenderEngine = "OpenRA.Renderer.Null.dll";
		public bool UseNullRenderer { get; set; }
	}
	
	public class SoundSettings
	{
		public float SoundVolume = 0.5f;
		public float MusicVolume = 0.5f;
		public float VideoVolume = 0.5f;
		public bool Shuffle = false;
		public bool Repeat = false;
	}
	
	public class PlayerSettings
	{
		public string Name = "Newbie";
		public Color Color1 = Color.FromArgb(255,160,238);
		public Color Color2 = Color.FromArgb(68,0,56);
		public string LastServer = "localhost:1234";
	}
	
	public class GameSettings
	{
		public string[] Mods = { "ra" };
		public bool MatchTimer = true;
		
		// Chat settings
		public bool TeamChatToggle = false;

		// Behaviour settings
        public bool ViewportEdgeScroll = true;
        public bool InverseDragScroll = false;
		public float ViewportEdgeScrollStep = 10f;

		// Internal game settings
		public int Timestep = 40;
		public int SheetSize = 2048;
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
		
		Dictionary<string, object> Sections;
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
				{"Debug", Debug}
			};
			
			// Should we run in dedicated mode (use the server extension)
			Server.IsDedicated = args.GetValue("Server.IsDedicated", false);
			if (Server.IsDedicated)
				Graphics.UseNullRenderer = args.GetValue("Graphics.UseNullRenderer", false);

			// Override fieldloader to ignore invalid entries
			var err1 = FieldLoader.UnknownFieldAction;
			var err2 = FieldLoader.InvalidValueAction;
			
			FieldLoader.UnknownFieldAction = (s,f) =>
			{
				Console.WriteLine( "Ignoring unknown field `{0}` on `{1}`".F( s, f.Name ) );
			};
			
			if (File.Exists(SettingsFile))
			{
				Console.WriteLine("Loading settings file {0}",SettingsFile);
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
			object defaults = Activator.CreateInstance(section.GetType());
			FieldLoader.InvalidValueAction = (s,t,f) =>
			{
				object ret = defaults.GetType().GetField(f).GetValue(defaults);
				System.Console.WriteLine("FieldLoader: Cannot parse `{0}` into `{2}:{1}`; substituting default `{3}`".F(s,t.Name,f,ret) );
				return ret;
			};
			
			FieldLoader.Load(section, yaml);
		}
	}
}
