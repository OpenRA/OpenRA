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
			
			var proc = self.traits.Get<Harvester>().LinkedProc;
			
			if (proc == null)
				return new Wait(10) { NextActivity = this };
			
			if( self.Location != proc.Location + proc.traits.Get<IAcceptOre>().DeliverOffset )
			{
				return new Move(proc.Location + proc.traits.Get<IAcceptOre>().DeliverOffset, 0) { NextActivity = this };
			}
			else if (!isDocking)
			{
				isDocking = true;
				proc.traits.Get<IAcceptOre>().OnDock(self, this);
			}
			return new Wait(10) { NextActivity = this };
		}

		public void Cancel(Actor self)
		{
			// TODO: allow canceling of deliver orders?
		}
	}
}
