#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class DeliverResources : IActivity
	{
		public IActivity NextActivity { get; set; }

		bool isDocking;

		public DeliverResources() { }

		public IActivity Tick( Actor self )
		{
			if( NextActivity != null )
				return NextActivity;

			var harv = self.Trait<Harvester>();

			if (harv.LinkedProc == null || !harv.LinkedProc.IsInWorld)
				harv.ChooseNewProc(self, null);

			if (harv.LinkedProc == null)	// no procs exist; check again in 1s.
				return new Wait(25) { NextActivity = this };

			var proc = harv.LinkedProc;
			
			if( self.Location != proc.Location + proc.Trait<IAcceptOre>().DeliverOffset )
			{
				return new Move(proc.Location + proc.Trait<IAcceptOre>().DeliverOffset, 0) { NextActivity = this };
			}
			else if (!isDocking)
			{
				isDocking = true;
				proc.Trait<IAcceptOre>().OnDock(self, this);
			}
			return new Wait(10) { NextActivity = this };
		}

		public void Cancel(Actor self)
		{
			// TODO: allow canceling of deliver orders?
		}
	}
}
