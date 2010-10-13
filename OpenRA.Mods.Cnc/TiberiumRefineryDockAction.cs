#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
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
			int2 startDock = harv.Trait<IHasLocation>().PxPosition;
			int2 endDock = self.Trait<IHasLocation>().PxPosition + new int2(-15,8);
			var harvester = harv.Trait<Harvester>();

			harv.QueueActivity( new Turn(112) );
			harv.QueueActivity( new CallFunc( () =>
			{
				if (!preventDock)
				{
					dockedHarv = harv;
					self.Trait<RenderBuilding>().PlayCustomAnim(self, "active");
					
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
					{
						harv.QueueActivity( new Move(harvester.LastHarvestedCell, 5) );
						if (harv.Owner == self.World.LocalPlayer)
							self.World.AddFrameEndTask( w =>
							{
								var line = harv.TraitOrDefault<DrawLineToTarget>();
								if (line != null)
									line.SetTargetSilently(harv, Target.FromCell(harvester.LastHarvestedCell), Color.Green);                           
							});
					}
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
			if (!harv.IsDead())
				harv.Trait<Harvester>().Visible = true;
		}
		
		public void Selling (Actor self) { CancelDock(self, dockedHarv); }
		public void Sold (Actor self) {}
		
		public void Damaged (Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				 CancelDock(self, dockedHarv);
		}
		
		public void OnCapture (Actor self, Actor captor, Player oldOwner, Player newOwner)
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
