#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	// Assumes you have Minelayer on that unit
	public class LayMines : Activity
	{
		readonly Minelayer minelayer;
		readonly MinelayerInfo info;
		readonly AmmoPool[] ammoPools;
		readonly IMove movement;
		readonly HashSet<string> rearmBuildings;

		public LayMines(Actor self)
		{
			minelayer = self.TraitOrDefault<Minelayer>();
			info = self.Info.TraitInfo<MinelayerInfo>();
			ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
			movement = self.Trait<IMove>();
			rearmBuildings = info.RearmBuildings;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if (ammoPools != null && ammoPools.Any(p => p.Info.Name == info.AmmoPoolName && !p.HasAmmo()))
			{
				// Rearm (and possibly repair) at rearm building, then back out here to refill the minefield some more
				var rearmTarget = self.World.Actors.Where(a => self.Owner.Stances[a.Owner] == Stance.Ally
					&& rearmBuildings.Contains(a.Info.Name))
					.ClosestTo(self);

				if (rearmTarget == null)
					return new Wait(20);

				return ActivityUtils.SequenceActivities(
					new MoveAdjacentTo(self, Target.FromActor(rearmTarget)),
					movement.MoveTo(self.World.Map.CellContaining(rearmTarget.CenterPosition), rearmTarget),
					new Rearm(self),
					new Repair(rearmTarget),
					this);
			}

			if (minelayer.Minefield.Contains(self.Location) && ShouldLayMine(self, self.Location))
			{
				LayMine(self);
				return ActivityUtils.SequenceActivities(new Wait(20), this); // A little wait after placing each mine, for show
			}

			if (minelayer.Minefield.Length > 0)
			{
				// Don't get stuck forever here
				for (var n = 0; n < 20; n++)
				{
					var p = minelayer.Minefield.Random(self.World.SharedRandom);
					if (ShouldLayMine(self, p))
						return ActivityUtils.SequenceActivities(movement.MoveTo(p, 0), this);
				}
			}

			// TODO: Return somewhere likely to be safe (near rearm building) so we're not sitting out in the minefield.
			return new Wait(20);	// nothing to do here
		}

		static bool ShouldLayMine(Actor self, CPos p)
		{
			// If there is no unit (other than me) here, we want to place a mine here
			return self.World.ActorMap.GetActorsAt(p).All(a => a == self);
		}

		void LayMine(Actor self)
		{
			if (ammoPools != null)
			{
				var pool = ammoPools.FirstOrDefault(x => x.Info.Name == info.AmmoPoolName);
				if (pool == null)
					return;
				pool.TakeAmmo();
			}

			self.World.AddFrameEndTask(
				w => w.CreateActor(info.Mine, new TypeDictionary
				{
					new LocationInit(self.Location),
					new OwnerInit(self.Owner),
				}));
		}
	}
}
