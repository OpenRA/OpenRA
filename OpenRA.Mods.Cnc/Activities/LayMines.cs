#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	// Assumes you have Minelayer on that unit
	public class LayMines : Activity
	{
		readonly MinelayerInfo info;
		readonly AmmoPool[] ammoPools;
		readonly IMove movement;
		readonly RearmableInfo rearmableInfo;
		readonly CPos[] minefield;

		public LayMines(Actor self, CPos[] minefield)
		{
			info = self.Info.TraitInfo<MinelayerInfo>();
			ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
			movement = self.Trait<IMove>();
			rearmableInfo = self.Info.TraitInfoOrDefault<RearmableInfo>();
			this.minefield = minefield;
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			if (IsCanceling)
				return NextActivity;

			if (rearmableInfo != null && ammoPools.Any(p => p.Info.Name == info.AmmoPoolName && !p.HasAmmo()))
			{
				// Rearm (and possibly repair) at rearm building, then back out here to refill the minefield some more
				var rearmTarget = self.World.Actors.Where(a => self.Owner.Stances[a.Owner] == Stance.Ally
					&& rearmableInfo.RearmActors.Contains(a.Info.Name))
					.ClosestTo(self);

				if (rearmTarget == null)
					return NextActivity;

				// Add a CloseEnough range of 512 to the Rearm/Repair activities in order to ensure that we're at the host actor
				QueueChild(self, new MoveAdjacentTo(self, Target.FromActor(rearmTarget)), true);
				QueueChild(self, movement.MoveTo(self.World.Map.CellContaining(rearmTarget.CenterPosition), rearmTarget));
				QueueChild(self, new Rearm(self, rearmTarget, new WDist(512)));
				QueueChild(self, new Repair(self, rearmTarget, new WDist(512)));
				return this;
			}

			if ((minefield == null || minefield.Contains(self.Location)) && ShouldLayMine(self, self.Location))
			{
				LayMine(self);
				QueueChild(self, new Wait(20), true); // A little wait after placing each mine, for show
				return this;
			}

			if (minefield != null && minefield.Length > 0)
			{
				// Don't get stuck forever here
				for (var n = 0; n < 20; n++)
				{
					var p = minefield.Random(self.World.SharedRandom);
					if (ShouldLayMine(self, p))
					{
						QueueChild(self, movement.MoveTo(p, 0), true);
						return this;
					}
				}
			}

			// TODO: Return somewhere likely to be safe (near rearm building) so we're not sitting out in the minefield.
			return NextActivity;
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
				pool.TakeAmmo(self, 1);
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
