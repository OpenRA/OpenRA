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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AntiAirInfo : ITraitInfo
	{
		public readonly float Badness = 1000f;
		public object Create( ActorInitializer init ) { return new AntiAir( init.self ); }
	}
	
	class AntiAir : IProvideHazard
	{
		public AntiAir(Actor self)
		{
			self.World.WorldActor.traits.Get<HazardLayer>().Add( self, this );
		}
		
		public IEnumerable<HazardLayer.Hazard> HazardCells(Actor self)
		{
			var info = self.Info.Traits.Get<AntiAirInfo>();
			return self.World.FindTilesInCircle(self.Location, (int)self.GetPrimaryWeapon().Range).Select(
			      	t => new HazardLayer.Hazard(){location = t, type = "antiair", intensity = info.Badness});
		}
	}
	
	class AvoidsAAInfo : TraitInfo<AvoidsAA> {}
	class AvoidsAA : IAvoidHazard
	{
		public string Type { get { return "antiair"; } }
	}
}
