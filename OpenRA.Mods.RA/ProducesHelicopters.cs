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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	public class ProducesHelicoptersInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init) { return new ProducesHelicopters(); }
	}
	
	class ProducesHelicopters : Production
	{
		// Hack around visibility bullshit in Production
		public override bool Produce( Actor self, ActorInfo producee )
		{
			var location = CreationLocation( self, producee );
			if( location == null || self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt( location.Value ).Any() )
				return false;

			var newUnit = self.World.CreateActor( producee.Name, location.Value, self.Owner );
			newUnit.traits.Get<Unit>().Facing = CreationFacing( self, newUnit ); ;

			var pi = self.Info.Traits.Get<ProductionInfo>();
			var rp = self.traits.GetOrDefault<RallyPoint>();
			if( rp != null || pi.ExitOffset != null)
			{
				if( newUnit.traits.Contains<Helicopter>() )
				{
					if (pi.ExitOffset != null)
						newUnit.QueueActivity(new Activities.HeliFly(Util.CenterOfCell(ExitLocation( self, producee ).Value)));
						
					if (rp != null)
						newUnit.QueueActivity( new Activities.HeliFly( Util.CenterOfCell(rp.rallyPoint)) );
				}
			}
			
			if (pi != null && pi.SpawnOffset != null)
				newUnit.CenterLocation = self.CenterLocation 
					+ new float2(pi.SpawnOffset[0], pi.SpawnOffset[1]);

			foreach (var t in self.traits.WithInterface<INotifyProduction>())
				t.UnitProduced(self, newUnit);

			Log.Write("debug", "{0} #{1} produced by {2} #{3}", newUnit.Info.Name, newUnit.ActorID, self.Info.Name, self.ActorID);

			return true;
		}
	}
}
