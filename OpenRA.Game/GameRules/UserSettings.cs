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

using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OpenRA.GameRules
{
	public class UserSettings
	{
		// Debug settings
		public bool UnitDebug = false;
		public bool PathDebug = false;
		public bool PerfDebug = false;
		public bool IndexDebug = false;
		public bool RecordSyncReports = true;
		
		// Window settings
		public WindowMode WindowMode = WindowMode.PseudoFullscreen;
		public int2 FullscreenSize = new int2(Screen.PrimaryScreen.Bounds.Width,Screen.PrimaryScreen.Bounds.Height);
		public int2 WindowedSize = new int2(1024,768);
		public readonly static int2 MinResolution = new int2(800, 600);
		
		//Sound Settings
		public float SoundVolume = 0.5f;
		public float MusicVolume = 0.5f;
		public bool MusicPlayer = false;
		
		// Internal game settings
		public int Timestep = 40;
		public int SheetSize = 2048;
		
		// External game settings
		public string LastServer = "localhost:1234";
		public string Replay = null;
		public string PlayerName = "Newbie";
		public string[] InitialMods = { "ra" };

		// Server settings
		public string LastServerTitle = "OpenRA Game";
		public int ListenPort = 1234;
		public int ExternalPort = 1234;
		public bool AdvertiseOnline = true;
		public string MasterServer = "http://open-ra.org/master/";
		
		string SettingsFile;
		UserSettings defaults;
		
		public UserSettings() {}
		public UserSettings(Settings args)
		{			
			defaults = new UserSettings();
			SettingsFile = Game.SupportDir + "settings.yaml";

			// Override settings loading to not crash
			var err1 = FieldLoader.UnknownFieldAction;
			var err2 = FieldLoader.InvalidValueAction;
			
			FieldLoader.InvalidValueAction = (s,t,f) =>
			{
				object ret = defaults.GetType().GetField(f).GetValue(defaults);
				System.Console.WriteLine("FieldLoader: Cannot parse `{0}` into `{2}:{1}`; substituting default `{3}`".F(s,t.Name,f,ret) );
				return ret;
			};
			
			FieldLoader.UnknownFieldAction = (s,f) =>
			{
				System.Console.WriteLine( "Ignoring unknown field `{0}` on `{1}`".F( s, f.Name ) );
			};
			
			if (File.Exists(SettingsFile))
			{
				System.Console.WriteLine("Loading settings file {0}",SettingsFile);
				var yaml = MiniYaml.FromFile(SettingsFile);
				FieldLoader.Load(this, yaml["Settings"]);
			}
			
			foreach (var f in this.GetType().GetFields())
				if (args.Contains(f.Name))
					OpenRA.FileFormats.FieldLoader.LoadField( this, f.Name, args.GetValue(f.Name, "") );
			
			FieldLoader.UnknownFieldAction = err1;
			FieldLoader.InvalidValueAction = err2;
		}
		
		public void Save()
		{
			Dictionary<string, MiniYaml> root = new Dictionary<string, MiniYaml>();
			root.Add("Settings", FieldSaver.SaveDifferences(this, defaults));
			root.WriteToFile(SettingsFile);
		}
	}
}
