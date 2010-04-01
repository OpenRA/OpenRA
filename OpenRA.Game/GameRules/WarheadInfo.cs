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
	public class WarheadInfo
	{
		public readonly int Spread = 1;
		public readonly float[] Verses = { 1, 1, 1, 1, 1 };
		public readonly bool Wall = false;
		public readonly bool Wood = false;
		public readonly bool Ore = false;
		public readonly int Explosion = 0;
		public readonly SmudgeType SmudgeType = SmudgeType.None;
		public readonly int[] SmudgeSize = { 0,0 };
		public readonly int InfDeath = 0;
		public readonly string ImpactSound = null;
		public readonly string WaterImpactSound = null;
		public readonly int Damage = 0;		// for new weapons infrastructure
		public readonly int Delay = 0;		// delay in ticks before dealing the damage. 0=instant

		public float EffectivenessAgainst(ArmorType at) { return Verses[ (int)at ]; }
	}

	public enum ArmorType
	{
		none = 0,
		wood = 1,
		light = 2,
		heavy = 3,
		concrete = 4,
	}

	public enum SmudgeType
	{
		None = 0,
		Crater = 1,
		Scorch = 2,
	}
}
