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
		public readonly bool UnitDebug = false;
		public readonly bool PathDebug = false;
		public readonly bool PerfGraph = true;
		
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
		public readonly string Map = "scm02ea.ini";
		public readonly int Player = 1;
		public readonly string Replay = "";
		public readonly string PlayerName = "";
		public readonly string[] InitialMods = { "ra" };
		
		// Gameplay options
		// TODO: These need to die
		public readonly bool RepairRequiresConyard = true;
		public readonly bool PowerDownBuildings = true;
	}
}
