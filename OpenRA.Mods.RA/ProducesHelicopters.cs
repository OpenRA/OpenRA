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
		
		
		// Hack around visibility bullshit in Production
		public override bool Produce( Actor self, ActorInfo producee )
		{
			// Pick an exit that we can move to
			var exit = int2.Zero;
			var spawn = float2.Zero;
			var success = false;
			
			// Pick a spawn/exit point
			// Todo: Reorder in a synced random way
			foreach (var s in Spawns)
			{
				exit = self.Location + s.Value;
				spawn = self.CenterLocation + s.Key;
				if (!self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt( exit ).Any())
				{
					success = true;
					break;
				}
			}
			
			if (!success)
				return false;

			// Todo: Once Helicopter supports it, update UIM if its docked/landed
			var newUnit = self.World.CreateActor( producee.Name, new TypeDictionary
			{
				new LocationInit( exit ),
				new OwnerInit( self.Owner ),
			});
			newUnit.CenterLocation = spawn;
        	
			var rp = self.traits.GetOrDefault<RallyPoint>();
			if( rp != null )
			{
				newUnit.QueueActivity( new Activities.HeliFly( Util.CenterOfCell(rp.rallyPoint)) );
			}
			
			foreach (var t in self.traits.WithInterface<INotifyProduction>())
				t.UnitProduced(self, newUnit);

			Log.Write("debug", "{0} #{1} produced by {2} #{3}", newUnit.Info.Name, newUnit.ActorID, self.Info.Name, self.ActorID);

			return true;
		}
	}
}
