#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System;

namespace OpenRA.FileFormats
{
	public class PlayerReference
	{
		public string Name;
		public string Palette;
		public bool OwnsWorld = false;
		public bool NonCombatant = false;
		public bool Playable = false;
		public string Bot = null;
		public bool DefaultStartingUnits = false;
		public bool AllowBots = true;
		public bool Required = false;

		public bool LockRace = false;
		public string Race;

		public bool LockColor = false;
		public ColorRamp ColorRamp = new ColorRamp(0,0,238,10);

		public bool LockSpawn = false;
		public int Spawn = 0;

		public bool LockTeam = false;
		public int Team = 0;

		public int InitialCash = 0;
		public string[] Allies = {};
		public string[] Enemies = {};

		public PlayerReference() {}
		public PlayerReference(MiniYaml my) { FieldLoader.Load(this, my); }

		public override string ToString() { return Name; }
	}
}
