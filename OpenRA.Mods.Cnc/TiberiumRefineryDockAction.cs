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
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.Cnc
{
	class TiberiumRefineryDockActionInfo : TraitInfo<TiberiumRefineryDockAction> {}
	class TiberiumRefineryDockAction : IAcceptOreDockAction, ITick, INotifyDamage, INotifySold, INotifyCapture
	{
		Actor dockedHarv = null;
		bool preventDock = false;
		public void OnDock(Actor self, Actor harv, DeliverResources dockOrder)
		{
			var mobile = harv.Trait<Mobile>();
			var harvester = harv.Trait<Harvester>();

			harv.QueueActivity( new Turn(112) );
			
			if (!preventDock)
			{
				harv.QueueActivity( new CallFunc( () => dockedHarv = harv, false ) );
				harv.QueueActivity( new HarvesterDockSequence(harv, self) );
				harv.QueueActivity( new CallFunc( () => dockedHarv = null, false ) );			
			}
			
			// Tell the harvester to start harvesting
			// TODO: This belongs on the harv idle activity
			harv.QueueActivity( new CallFunc( () =>
			{
				if (harvester.LastHarvestedCell != int2.Zero)
				{
					harv.QueueActivity( mobile.MoveTo(harvester.LastHarvestedCell, 5) );
					harv.SetTargetLine(Target.FromCell(harvester.LastHarvestedCell), Color.Red, false);
				}
				harv.QueueActivity( new Harvest() );
			}));
		}
		
		public void Tick(Actor self)
		{
			// Harvester was killed while unloading
			if (dockedHarv != null && dockedHarv.IsDead())
			{
				self.Trait<RenderBuilding>().CancelCustomAnim(self);
				dockedHarv = null;
			}
		}
		
		void CancelDock(Actor self)
		{
			preventDock = true;

			// Cancel the dock sequence
			if (dockedHarv != null && !dockedHarv.IsDead())
				dockedHarv.CancelActivity();
		}
		
		public void Selling (Actor self) { CancelDock(self); }
		public void Sold (Actor self) {}
		
		public void Damaged (Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				 CancelDock(self);
		}
		
		public void OnCapture (Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (dockedHarv != null)
				dockedHarv.ChangeOwner(newOwner);
		}
	}
}
