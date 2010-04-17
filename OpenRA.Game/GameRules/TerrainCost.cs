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

using System;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.GameRules
{
	public enum UnitMovementType : byte
	{
		Foot = 0,
		Track = 1,
		Wheel = 2,
		Float = 3,
		Fly = 4,
	}

	public class TerrainCost
	{
		public readonly bool Buildable = true;
		public readonly float Foot = 0, Track = 0, Wheel = 0, Float = 0;
		public readonly bool AcceptSmudge = true;

		public TerrainCost(MiniYaml y) { FieldLoader.Load(this, y); }
		
		public float GetSpeedModifier(UnitMovementType umt)
		{
			switch (umt)			/* todo: make this nice */
			{
				case UnitMovementType.Fly: return 1;
				case UnitMovementType.Foot: return Foot;
				case UnitMovementType.Wheel: return Wheel;
				case UnitMovementType.Track: return Track;
				case UnitMovementType.Float: return Float;
				default:
					throw new InvalidOperationException("wtf?");
			}
		}
		
		public float GetCost(UnitMovementType umt)
		{
			switch (umt)			/* todo: make this nice */
			{
				case UnitMovementType.Fly: return 1;
				case UnitMovementType.Foot: return 1 / Foot;
				case UnitMovementType.Wheel: return 1 / Wheel;
				case UnitMovementType.Track: return 1 / Track;
				case UnitMovementType.Float: return 1 / Float;
				default:
					throw new InvalidOperationException("wtf?");
			}
		}
	}
}
