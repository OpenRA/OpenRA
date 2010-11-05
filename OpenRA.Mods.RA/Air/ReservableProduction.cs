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
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	// a small hack to teach Production about Reservable.

	public class ReservableProductionInfo : ProductionInfo, ITraitPrerequisite<ReservableInfo>
	{
		public override object Create(ActorInitializer init) { return new ReservableProduction(this); }
	}

	class ReservableProduction : Production
	{
		public ReservableProduction(ReservableProductionInfo info) : base(info) {}

		public override bool Produce(Actor self, ActorInfo producee)
		{
			if (Reservable.IsReserved(self))
				return false;

			// Pick a spawn/exit point
			// Todo: Reorder in a synced random way
			foreach (var s in self.Info.Traits.WithInterface<ExitInfo>())
			{
				var exit = self.Location + s.ExitCell;
				if (!self.World.WorldActor.Trait<UnitInfluence>().GetUnitsAt( exit ).Any( x => x != self ))
				{
					var newUnit = self.World.CreateActor( producee.Name, new TypeDictionary
					{
						new LocationInit( exit ),
						new OwnerInit( self.Owner ),
					});
		        	
					var rp = self.TraitOrDefault<RallyPoint>();
					if( rp != null )
					{
						newUnit.QueueActivity( new HeliFly( Util.CenterOfCell(rp.rallyPoint)) );
					}
					
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, exit);
		
					//Log.Write("debug", "{0} #{1} produced by {2} #{3}", newUnit.Info.Name, newUnit.ActorID, self.Info.Name, self.ActorID);
					return true;
				}
			}
			return false;
		}
	}
}
