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
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	public class AircraftInfo : ITraitInfo
	{
		public readonly int CruiseAltitude = 20;
		public readonly string[] RepairBuildings = { "fix" };
		public readonly string[] RearmBuildings = { "hpad", "afld" };

		public virtual object Create( ActorInitializer init ) { return new Aircraft( init ); }
	}

	public class Aircraft : IOccupySpace, IMovement
	{
		[Sync]
		public int2 Location;

		public Aircraft( ActorInitializer init )
		{
			this.Location = init.location;
		}

		public int2 TopLeft
		{
			get { return Location; }
		}

		public IEnumerable<int2> OccupiedCells()
		{
			// TODO: make helis on the ground occupy a space.
			yield break;
		}

		public bool AircraftCanEnter(Actor self, Actor a)
		{
			var aircraft = self.Info.Traits.Get<AircraftInfo>();
			return aircraft.RearmBuildings.Contains( a.Info.Name )
				|| aircraft.RepairBuildings.Contains( a.Info.Name );
		}

		public UnitMovementType GetMovementType() { return UnitMovementType.Fly; }
		public bool CanEnterCell(int2 location) { return true; }
	}
}
