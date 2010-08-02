#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
	public class ProducesHelicoptersInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init) { return new ProducesHelicopters(this); }
	}
	
	class ProducesHelicopters : Production
	{
		public ProducesHelicopters(ProducesHelicoptersInfo info) : base(info) {}
		
		/*
		// Hack around visibility bullshit in Production
		public override bool Produce( Actor self, ActorInfo producee )
		{
			var location = CreationLocation( self, producee );
			if( location == null || self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt( location.Value ).Any() )
				return false;
			
			var pi = self.Info.Traits.Get<ProductionInfo>();
			var newUnit = self.World.CreateActor( producee.Name, new TypeDictionary
			{
				new LocationInit( location.Value ),
				new OwnerInit( self.Owner ),
				new FacingInit( pi.ProductionFacing ),
			});
			                                     
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
		*/
	}
}
