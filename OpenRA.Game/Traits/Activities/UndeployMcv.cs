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

using System;
using OpenRA.Traits;

namespace OpenRA.Traits.Activities
{
	class UndeployMcv : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool started;

		void DoUndeploy(World w,Actor self)
		{
			self.Health = 0;
			foreach (var ns in self.traits.WithInterface<INotifySold>())
				ns.Sold(self);
			w.Remove(self);
			
			var mcv = w.CreateActor("mcv", self.Location + new int2(1, 1), self.Owner);
			mcv.traits.Get<Unit>().Facing = 96;
		}

		public IActivity Tick(Actor self)
		{
			if (!started)
			{
				var rb = self.traits.Get<RenderBuilding>();
				rb.PlayCustomAnimBackwards(self, "make",
					() => self.World.AddFrameEndTask(w => DoUndeploy(w,self)));
				
				foreach (var s in self.Info.Traits.Get<BuildingInfo>().SellSounds)
					Sound.PlayToPlayer(self.Owner, s);
				
				started = true;
			}

			return this;
		}

		public void Cancel(Actor self)
		{
			// Cancel can't happen between this being moved to the head of the list, and it being Ticked.
			throw new InvalidOperationException("UndeployMcvAction: Cancel() should never occur.");
		}
	}
}
