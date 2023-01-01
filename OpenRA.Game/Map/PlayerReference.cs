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
using OpenRA.Primitives;

namespace OpenRA
{
	public class PlayerReference
	{
		public string Name;
		public string Palette;
		public string Bot = null;
		public string StartingUnitsClass = null;
		public bool AllowBots = true;
		public bool Playable = false;
		public bool Required = false;
		public bool OwnsWorld = false;
		public bool Spectating = false;
		public bool NonCombatant = false;

		public bool LockFaction = false;
		public string Faction;

		public bool LockColor = false;
		public Color Color = Game.ModData.Manifest.Get<DefaultPlayer>().Color;

		/// <summary>
		/// Sets the "Home" location, which can be used by traits and scripts to e.g. set the initial camera
		/// location or choose the map edge for reinforcements.
		/// This will usually be overridden for client (lobby slot) players with a location based on the Spawn index
		/// </summary>
		public CPos HomeLocation = CPos.Zero;

		public bool LockSpawn = false;

		/// <summary>
		/// Sets the initial spawn point index that is used to override the "Home" location for client (lobby slot) players.
		/// Map players always ignore this and use HomeLocation directly.
		/// </summary>
		public int Spawn = 0;

		public bool LockTeam = false;
		public int Team = 0;

		public bool LockHandicap = false;
		public int Handicap = 0;

		public string[] Allies = Array.Empty<string>();
		public string[] Enemies = Array.Empty<string>();

		public PlayerReference() { }
		public PlayerReference(MiniYaml my) { FieldLoader.Load(this, my); }

		public override string ToString() { return Name; }
	}
}
