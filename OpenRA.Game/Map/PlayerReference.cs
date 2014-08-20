#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;

namespace OpenRA
{
	public class PlayerReference
	{
		public string Name;
		public string Palette;
		public bool OwnsWorld = false;
		public bool NonCombatant = false;
		public bool Playable = false;
		public bool Spectating = false;
		public string Bot = null;
		public string StartingUnitsClass = null;
		public bool AllowPlayers = true;
		public bool AllowBots = true;
		public bool Required = false;

		public bool LockRace = false;
		public string Race;

		// ColorRamp naming retained for backward compatibility
		public bool LockColor = false;
		public HSLColor ColorRamp = new HSLColor(0, 0, 238);
		public HSLColor Color { get { return ColorRamp; } set { ColorRamp = value; } }

		public bool LockSpawn = false;
		public int Spawn = 0;

		public bool LockTeam = false;
		public int Team = 0;

		public string[] Allies = { };
		public string[] Enemies = { };

		public PlayerReference() { }
		public PlayerReference(MiniYaml my) { FieldLoader.Load(this, my); }

		public override string ToString() { return Name; }
	}
}
