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

using System.Linq;
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
