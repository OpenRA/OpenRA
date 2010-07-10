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

namespace OpenRA.GameRules
{
	public class UserSettings
	{
		// Debug settings
		public bool UnitDebug = false;
		public bool PathDebug = true;
		public bool PerfDebug = true;
		public bool IndexDebug = false;
		public bool RecordSyncReports = true;
		
		// Window settings
		public readonly int Width = 0;
		public readonly int Height = 0;
		public readonly WindowMode WindowMode = WindowMode.PseudoFullscreen;
		public bool MusicPlayer = true;
		
		// Internal game settings
		public readonly int Timestep = 40;
		public readonly int SheetSize = 2048;
		
		// External game settings
		public readonly string NetworkHost = null;
		public readonly int NetworkPort = 0;
		public readonly string Replay = null;
		public readonly string PlayerName = null;
		public readonly string[] InitialMods = { "ra" };

		public readonly string GameName = "OpenRA Game";
		public readonly int ListenPort = 1234;
		public readonly int ExternalPort = 1234;
		public readonly bool InternetServer = true;
		public readonly string MasterServer = "http://open-ra.org/master/";
		
		string SettingsFile;
		UserSettings defaults;
		
		public UserSettings() {}
		public UserSettings(Settings args)
		{			
			defaults = new UserSettings();
			SettingsFile = Game.SupportDir + "settings.yaml";

			if (File.Exists(SettingsFile))
			{
				System.Console.WriteLine("Loading settings file {0}",SettingsFile);
				var yaml = MiniYaml.FromFile(SettingsFile);
				FieldLoader.Load(this, yaml["Settings"]);
			}
			
			foreach (var f in this.GetType().GetFields())
				if (args.Contains(f.Name))
					OpenRA.FileFormats.FieldLoader.LoadField( this, f.Name, args.GetValue(f.Name, "") );
		}
		
		public void Save()
		{
			Dictionary<string, MiniYaml> root = new Dictionary<string, MiniYaml>();
			root.Add("Settings", FieldSaver.SaveDifferences(this, defaults));
			root.WriteToFile(SettingsFile);
		}
	}
}
