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

namespace OpenRA.GameRules
{
	public class UserSettings
	{
		// Debug settings
		public bool UnitDebug = false;
		public bool PathDebug = false;
		public bool PerfGraph = true;
		public bool PerfText = true;
		public bool IndexDebug = false;
		
		// Window settings
		public readonly int Width = 0;
		public readonly int Height = 0;
		public readonly bool Fullscreen = false;
		
		// Internal game settings
		public readonly int Timestep = 40;
		public readonly int SheetSize = 512;
		
		// External game settings
		public readonly string NetworkHost = "";
		public readonly int NetworkPort = 0;
		public readonly string Map = "testmap.yaml";
		public readonly int Player = 1;
		public readonly string Replay = "";
		public readonly string PlayerName = "";
		public readonly string[] InitialMods = { "ra" };

		public readonly string GameName = "OpenRA Game";
		public readonly int ListenPort = 1234;
		public readonly int ExternalPort = 1234;
		public readonly bool InternetServer = true;
		public readonly string MasterServer = "http://open-ra.org/master/";
	}
}
