#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class AircraftInfo : ITraitInfo
	{
		public readonly int CruiseAltitude = 20;
		[ActorReference]
		public readonly string[] RepairBuildings = { "fix" };
		[ActorReference]
		public readonly string[] RearmBuildings = { "hpad", "afld" };

		public virtual object Create( ActorInitializer init ) { return new Aircraft( init ); }
	}

	public class Aircraft : IMove, IOccupySpace
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

		public void SetPosition(Actor self, int2 cell)
		{
			Location = cell;
			self.CenterLocation = Util.CenterOfCell(cell);
		}
		
		public bool AircraftCanEnter(Actor self, Actor a)
		{
			var aircraft = self.Info.Traits.Get<AircraftInfo>();
			return aircraft.RearmBuildings.Contains( a.Info.Name )
				|| aircraft.RepairBuildings.Contains( a.Info.Name );
		}

		public virtual IEnumerable<float2> GetCurrentPath(Actor self)
		{
			var move = self.GetCurrentActivity() as Activities.Fly;
			if (move == null) return new float2[] { };
			
			return new float2[] { move.Pos };
		}
		
		public bool CanEnterCell(int2 location) { return true; }
		
		public float MovementCostForCell(Actor self, int2 cell) { return 1f; }
		
		public float MovementSpeedForCell(Actor self, int2 cell)
		{		
			var unitInfo = self.Info.Traits.GetOrDefault<UnitInfo>();
			if( unitInfo == null)
			   return 0f;
			
			var modifier = self.traits
				.WithInterface<ISpeedModifier>()
				.Select(t => t.GetSpeedModifier())
				.Product();
			return unitInfo.Speed * modifier;
		}
		
		int2[] noCells = new int2[] { };
		public IEnumerable<int2> OccupiedCells() { return noCells; }
	}
}
