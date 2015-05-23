#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	// assumes you have Minelayer on that unit
	class LayMines : Activity
	{
		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;

			var movement = self.Trait<IMove>();
			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (!limitedAmmo.HasAmmo())
			{
				var info = self.Info.Traits.Get<MinelayerInfo>();

				// rearm & repair at fix, then back out here to refill the minefield some more
				var buildings = info.RearmBuildings;
				var rearmTarget = self.World.Actors.Where(a => self.Owner.Stances[a.Owner] == Stance.Ally
					&& buildings.Contains(a.Info.Name))
					.ClosestTo(self);

				if (rearmTarget == null)
					return new Wait(20);

				return Util.SequenceActivities(
					new MoveAdjacentTo(self, Target.FromActor(rearmTarget)),
					movement.MoveTo(self.World.Map.CellContaining(rearmTarget.CenterPosition), rearmTarget),
					new Rearm(self, info.RearmSound),
					new Repair(rearmTarget),
					this);
			}

			var ml = self.Trait<Minelayer>();
			if (ml.Minefield.Contains(self.Location) && ShouldLayMine(self, self.Location))
			{
				LayMine(self);
				return Util.SequenceActivities(new Wait(20), this); // a little wait after placing each mine, for show
			}

			if (ml.Minefield.Length > 0)
			{
				// dont get stuck forever here
				for (var n = 0; n < 20; n++)		
				{
					var p = ml.Minefield.Random(self.World.SharedRandom);
					if (ShouldLayMine(self, p))
						return Util.SequenceActivities(movement.MoveTo(p, 0), this);
				}
			}

			// TODO: return somewhere likely to be safe (near fix) so we're not sitting out in the minefield.
			return new Wait(20);	// nothing to do here
		}

		static bool ShouldLayMine(Actor self, CPos p)
		{
			// if there is no unit (other than me) here, we want to place a mine here
			return !self.World.ActorMap.GetUnitsAt(p).Any(a => a != self);
		}

		static void LayMine(Actor self)
		{
			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null)
				limitedAmmo.TakeAmmo();

			self.World.AddFrameEndTask(
				w => w.CreateActor(self.Info.Traits.Get<MinelayerInfo>().Mine, new TypeDictionary
				{
					new LocationInit(self.Location),
					new OwnerInit(self.Owner),
				}));
		}
	}
}
