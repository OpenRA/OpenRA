#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;

namespace OpenRA.FileFormats
{
	public class PlayerReference
	{
		public string Name;
		public string Palette;
		public string Race;
		public bool OwnsWorld = false;
		public bool NonCombatant = false;
		public bool Playable = false;
		public bool DefaultStartingUnits = false;
		public Color Color = Color.FromArgb(238,238,238);
		public Color Color2 = Color.FromArgb(44,28,24);
		
		public int InitialCash = 0;
		public string[] Allies = {};
		public string[] Enemies = {};
		
		public PlayerReference() {}
		public PlayerReference(MiniYaml my)
		{
			FieldLoader.Load(this, my);
		}
		
		public PlayerReference(string name, string race, bool ownsworld, bool noncombatant)
		{
			Name = name;
			Race = race;
			OwnsWorld = ownsworld;
			NonCombatant = noncombatant;
		}
	}
}
