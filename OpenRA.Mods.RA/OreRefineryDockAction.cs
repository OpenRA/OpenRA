#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class OreRefineryDockActionInfo : TraitInfo<OreRefineryDockAction> {}

	class OreRefineryDockAction : IAcceptOreDockAction, INotifyCapture
	{
	
		Actor dockedHarv = null;
		public void OnDock(Actor self, Actor harv, DeliverResources dockOrder)
		{
			var unit = harv.traits.Get<Unit>();
			var harvester = harv.traits.Get<Harvester>();

			if (unit.Facing != 64)
				harv.QueueActivity (new Turn (64));
			
			harv.QueueActivity (new CallFunc (() =>
			{
				dockedHarv = harv;
				var renderUnit = harv.traits.Get<RenderUnit> ();
				if (renderUnit.anim.CurrentSequence.Name != "empty")
					renderUnit.PlayCustomAnimation (harv, "empty", () =>
					{
						harvester.Deliver(harv, self);
						harv.QueueActivity( new CallFunc( () => dockedHarv = null, false ) );

						if (harvester.LastHarvestedCell != int2.Zero)
							harv.QueueActivity( new Move(harvester.LastHarvestedCell, 5) );
						harv.QueueActivity( new Harvest() );
					});
			}));
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
