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
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	// assumes you have Minelayer on that unit

	class LayMines : CancelableActivity
	{
		public override IActivity Tick( Actor self )
		{
			if (IsCanceled) return NextActivity;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (!limitedAmmo.HasAmmo())
			{
				// rearm & repair at fix, then back out here to refill the minefield some more
				var buildings = self.Info.Traits.Get<MinelayerInfo>().RearmBuildings;
				var rearmTarget = self.World.Actors.FirstOrDefault(a => a.Owner != null && self.Owner.Stances[a.Owner] == Stance.Ally
					&& buildings.Contains(a.Info.Name));

				if (rearmTarget == null)
					return new Wait(20);

				return Util.SequenceActivities(
					new Enter(rearmTarget),
					//new Move(Util.CellContaining(rearmTarget.CenterLocation), rearmTarget),
					new Rearm(),
					new Repair(rearmTarget),
					this );
			}

			var ml = self.Trait<Minelayer>();
			if (ml.minefield.Contains(self.Location) &&
				ShouldLayMine(self, self.Location))
			{
				LayMine(self);
				return Util.SequenceActivities( new Wait(20), this ); // a little wait after placing each mine, for show
			}

			for (var n = 0; n < 20; n++)		// dont get stuck forever here
			{
				var p = ml.minefield.Random(self.World.SharedRandom);
				if (ShouldLayMine(self, p))
					return Util.SequenceActivities( new Move(p, 0), this );
			}

			// todo: return somewhere likely to be safe (near fix) so we're not sitting out in the minefield.

			return new Wait(20);	// nothing to do here
		}

		bool ShouldLayMine(Actor self, int2 p)
		{
			// if there is no unit (other than me) here, we want to place a mine here
			return !self.World.WorldActor.Trait<UnitInfluence>()
				.GetUnitsAt(p).Any(a => a != self);
		}

		void LayMine(Actor self)
		{
			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null) limitedAmmo.Attacking(self);

			self.World.AddFrameEndTask(
				w => w.CreateActor(self.Info.Traits.Get<MinelayerInfo>().Mine, new TypeDictionary
				{
					new LocationInit( self.Location ),
					new OwnerInit( self.Owner ),
				}));
		}
	}
}
