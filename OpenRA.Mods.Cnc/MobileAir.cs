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

namespace OpenRA.Traits
{
	public class MobileAirInfo : MobileInfo
	{
		public readonly int CruiseAltitude = 20;
		public readonly float InstabilityMagnitude = 2.0f;
		public readonly int InstabilityTicks = 5;	
		public readonly bool LandWhenIdle = true;
		
		public override object Create(ActorInitializer init) { return new MobileAir(init, this); }
	}
	
	public class MobileAir : Mobile, ITick, IOccupyAir
	{
		MobileAirInfo AirInfo;
		public MobileAir (ActorInitializer init, MobileAirInfo info)
			: base(init, info)
		{
			AirInfo = info;
		}

		public override void AddInfluence()
		{
			self.World.WorldActor.traits.Get<AircraftInfluence>().Add( self, this );
		}
		
		public override void RemoveInfluence()
		{
			self.World.WorldActor.traits.Get<AircraftInfluence>().Remove( self, this );
		}
		
		public override bool CanEnterCell(int2 p, Actor ignoreBuilding, bool checkTransientActors)
		{
			if (!checkTransientActors)
				return true;
			
			return self.World.WorldActor.traits.Get<AircraftInfluence>().GetUnitsAt(p).Count() == 0;
		}
		
		public override void FinishedMoving(Actor self) {}
		
		public override float MovementCostForCell(Actor self, int2 cell)
		{
			return (!self.World.Map.IsInMap(cell.X,cell.Y)) ? float.PositiveInfinity : 0;
		}

		public override float MovementSpeedForCell(Actor self, int2 cell)
		{		
			var unitInfo = self.Info.Traits.GetOrDefault<UnitInfo>();
			if( unitInfo == null )
			   return 0f;
			
			var modifier = self.traits
				.WithInterface<ISpeedModifier>()
				.Select(t => t.GetSpeedModifier())
				.Product();
			return unitInfo.Speed * modifier;
		}
		
		public override IEnumerable<int2> OccupiedCells()
		{
			// Todo: do the right thing when landed
			return new int2[] {};
		}
		
		public IEnumerable<int2> OccupiedAirCells()
		{
			return (fromCell == toCell)
				? new[] { fromCell }
				: CanEnterCell(toCell)
					? new[] { toCell }
					: new[] { fromCell, toCell };
		}
		
		int offsetTicks = 0;
		public void Tick(Actor self)
		{
			var unit = self.traits.Get<Unit>();
			//if (unit.Altitude <= 0)
			//	return;
			
			if (unit.Altitude < AirInfo.CruiseAltitude)
				unit.Altitude++;
			
			if (--offsetTicks <= 0)
			{
				self.CenterLocation += AirInfo.InstabilityMagnitude * self.World.SharedRandom.Gauss2D(5);
				unit.Altitude += (int)(AirInfo.InstabilityMagnitude * self.World.SharedRandom.Gauss1D(5));
				offsetTicks = AirInfo.InstabilityTicks;
			}
		}
	}
}