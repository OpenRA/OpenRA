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

using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Cnc
{
	class TiberiumRefineryDockActionInfo : TraitInfo<TiberiumRefineryDockAction> {}

	class TiberiumRefineryDockAction : IAcceptOreDockAction, INotifyDamage, INotifySold, INotifyCapture
	{
		Actor dockedHarv = null;
		bool preventDock = false;
		public void OnDock(Actor self, Actor harv, DeliverResources dockOrder)
		{
			float2 startDock = harv.CenterLocation;
			float2 endDock = self.CenterLocation + new float2(-15,8);
			var harvester = harv.traits.Get<Harvester>();

			harv.QueueActivity( new Turn(112) );
			harv.QueueActivity( new CallFunc( () =>
			{
				if (!preventDock)
				{
					dockedHarv = harv;
					self.traits.Get<RenderBuilding>().PlayCustomAnim(self, "active");
					
					harv.QueueActivity( new Drag(startDock, endDock, 12) );
					harv.QueueActivity( new CallFunc( () =>
					{
						self.World.AddFrameEndTask( w1 =>
						{
							if (!preventDock)
								harvester.Visible = false;
							harvester.Deliver(harv, self);
						});
					}, false ) );
					harv.QueueActivity( new Wait(18, false ) );
					harv.QueueActivity( new CallFunc( () => harvester.Visible = true, false ) );
					harv.QueueActivity( new Drag(endDock, startDock, 12) );
					harv.QueueActivity( new CallFunc( () => dockedHarv = null, false ) );
					if (harvester.LastHarvestedCell != int2.Zero)
						harv.QueueActivity( new Move(harvester.LastHarvestedCell, 5) );
				}
				harv.QueueActivity( new Harvest() );	
			}) );
		}
		
		void CancelDock(Actor self, Actor harv)
		{
			preventDock = true;
			if (dockedHarv == null)
				return;
			
			// invisible harvester makes ceiling cat cry
			harv.traits.Get<Harvester>().Visible = true;
		}
		
		public void Selling (Actor self) { CancelDock(self, dockedHarv); }
		public void Sold (Actor self) {}
		
		public void Damaged (Actor self, AttackInfo e)
		{
			if (self.IsDead)
				 CancelDock(self, dockedHarv);
		}
		
		public void OnCapture (Actor self, Actor captor)
		{
			if (dockedHarv == null)
				return;
			
			dockedHarv.World.AddFrameEndTask(w =>
			{
				// momentarily remove from world so the ownership queries don't get confused
				w.Remove(dockedHarv);
				dockedHarv.Owner = captor.Owner;
				w.Add(dockedHarv);
			});
		}
	}
}
