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
	public class ProjectileInfo
	{
		public readonly bool AA = false;
		public readonly bool AG = true;
		public readonly bool ASW = false;
		public readonly bool Arcing = false;
		public readonly int Arm = 0;
		public readonly bool Degenerates = false;
		public readonly bool High = false;
		public readonly string Image = null;
		public readonly bool Inaccurate = false;
		public readonly bool Parachuted = false;
		public readonly bool Proximity = false;
		public readonly int ROT = 0;
		public readonly bool Shadow = true;
		public readonly bool UnderWater = false;
		public readonly int RangeLimit = 0;

		// OpenRA-specific:
		public readonly string Trail = null;
	}
}
