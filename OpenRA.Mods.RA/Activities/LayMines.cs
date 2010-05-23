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
	// assumes you have Minelayer on that unit

	class LayMines : IActivity
	{
		bool canceled = false;
		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			if (canceled) return NextActivity;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (!limitedAmmo.HasAmmo())
			{
				// rearm & repair at fix, then back out here to refill the minefield some more
				var buildings = self.Info.Traits.Get<MinelayerInfo>().RearmBuildings;
				var rearmTarget = self.World.Actors.FirstOrDefault(a => self.Owner.Stances[a.Owner] == Stance.Ally
					&& buildings.Contains(a.Info.Name));

				if (rearmTarget == null)
					return new Wait(20);

				return new Move(((1 / 24f) * rearmTarget.CenterLocation).ToInt2(), rearmTarget)
					{ NextActivity = new Rearm() { NextActivity = new Repair() { NextActivity = this } } };
			}

			var ml = self.traits.Get<Minelayer>();
			if (ml.minefield.Contains(self.Location) &&
				ShouldLayMine(self, self.Location))
			{
				LayMine(self);
				return new Wait(20) { NextActivity = this };	// a little wait after placing each mine, for show
			}

			for (var n = 0; n < 20; n++)		// dont get stuck forever here
			{
				var p = ml.minefield.Random(self.World.SharedRandom);
				if (ShouldLayMine(self, p))
					return new Move(p, 0) { NextActivity = this };
			}

			// todo: return somewhere likely to be safe (near fix) so we're not sitting out in the minefield.

			return new Wait(20);	// nothing to do here
		}

		bool ShouldLayMine(Actor self, int2 p)
		{
			// if there is no unit (other than me) here, we want to place a mine here
			return !self.World.WorldActor.traits.Get<UnitInfluence>()
				.GetUnitsAt(p).Any(a => a != self);
		}

		void LayMine(Actor self)
		{
			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null) limitedAmmo.Attacking(self);

			self.World.AddFrameEndTask(
				w => w.CreateActor(
					self.Info.Traits.Get<MinelayerInfo>().Mine, self.Location, self.Owner));
		}

		public void Cancel( Actor self ) { canceled = true; NextActivity = null; }
	}
}
