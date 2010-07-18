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
