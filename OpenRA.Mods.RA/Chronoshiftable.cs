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

using OpenRA.Traits;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	class ChronoshiftableInfo : TraitInfo<Chronoshiftable> { }

	public class Chronoshiftable : ITick
	{
		// Return-to-sender logic
		[Sync]
		int2 chronoshiftOrigin;
		[Sync]
		int chronoshiftReturnTicks = 0;

		public void Tick(Actor self)
		{
			if (chronoshiftReturnTicks <= 0)
				return;

			if (chronoshiftReturnTicks > 0)
				chronoshiftReturnTicks--;

			// Return to original location
			if (chronoshiftReturnTicks == 0)
			{
				self.CancelActivity();
				// Todo: need a new Teleport method that will move to the closest available cell
				self.QueueActivity(new Teleport(chronoshiftOrigin));
			}
		}

		public virtual bool Activate(Actor self, int2 targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			/// Set up return-to-sender info
			chronoshiftOrigin = self.Location;
			chronoshiftReturnTicks = duration;
			
			// Kill cargo
			if (killCargo && self.traits.Contains<Cargo>())
			{
				var cargo = self.traits.Get<Cargo>();
				while (!cargo.IsEmpty(self))
				{
					chronosphere.Owner.Kills++;
					var a = cargo.Unload(self);
					a.Owner.Deaths++;
				}
			}

			// Set up the teleport
			self.CancelActivity();
			self.QueueActivity(new Teleport(targetLocation));
			
			return true;
		}
	}
}
