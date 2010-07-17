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
