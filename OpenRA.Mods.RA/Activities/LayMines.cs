#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.FileFormats;

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
				var rearmTarget = self.World.Actors.FirstOrDefault(a => a.Owner != null && self.Owner.Stances[a.Owner] == Stance.Ally
					&& buildings.Contains(a.Info.Name));

				if (rearmTarget == null)
					return new Wait(20);

				return new Move(Util.CellContaining(rearmTarget.CenterLocation), rearmTarget)
					{ NextActivity = new Rearm() { NextActivity = new Repair(rearmTarget) { NextActivity = this } } };
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
				w => w.CreateActor(self.Info.Traits.Get<MinelayerInfo>().Mine, new TypeDictionary
				{
					new LocationInit( self.Location ),
					new OwnerInit( self.Owner ),
				}));
		}

		public void Cancel( Actor self ) { canceled = true; NextActivity = null; }
	}
}
