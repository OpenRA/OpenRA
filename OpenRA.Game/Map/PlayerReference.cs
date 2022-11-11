#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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

		private bool playable = false;

		public bool Playable
		{
			get { return playable; }
			set { playable = value; }
		}

		private bool required = false;
		public bool Required
		{
			get { return required; }
			set { required = value; }
		}

		private bool ownsWorld = false;

		public bool OwnsWorld
		{
			get { return ownsWorld; }
			set { ownsWorld = value; }
		}

		private bool spectating = false;

		public bool Spectating
		{
			get { return spectating; }
			set { spectating = value; }
		}

		private bool nonCombatant = false;

		public bool NonCombatant
		{
			get { return nonCombatant; }
			set { nonCombatant = value; }
		}

		private bool lockFaction = false;

		public bool LockFaction
		{
			get { return lockFaction; }
			set { lockFaction = value; }
		}


		private string faction;

		public string Faction
		{
			get { return faction; }
			set { faction = value; }
		}

		private bool lockColor = false;

		public bool LockColor
		{
			get { return lockColor; }
			set { lockColor = value; }
		}

		private Color color = Game.ModData.Manifest.Get<DefaultPlayer>().Color;

		public Color Color
		{
			get { return color; }
			set { color = value; }
		}

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
