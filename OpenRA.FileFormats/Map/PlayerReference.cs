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
		public readonly string Name;
		public readonly string Palette;
		public readonly string Race;
		public readonly bool OwnsWorld = false;
		public readonly bool NonCombatant = false;
		public readonly Color Color = Color.FromArgb(238,238,238);
		public readonly Color Color2 = Color.FromArgb(44,28,24);
		
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
