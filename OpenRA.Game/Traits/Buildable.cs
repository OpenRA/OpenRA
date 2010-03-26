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

namespace OpenRA.Traits
{
	class BuildableInfo : StatelessTraitInfo<Buildable>
	{
		public readonly int TechLevel = -1;
		public readonly string[] Prerequisites = { };
		public readonly string[] BuiltAt = { };
		public readonly string[] Owner = { };
		public readonly int Cost = 0;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		public readonly string Icon = null;
		public readonly string[] AlternateName = { };
		public readonly int BuildPaletteOrder = 50;
	}

	class Buildable { }
}
