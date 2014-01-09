#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class MoveAdjacentTo : Activity
	{
		readonly Target target;
		readonly Mobile mobile;
		readonly PathFinder pathFinder;

		public MoveAdjacentTo(Actor self, Target target)
		{
			this.target = target;
			mobile = self.Trait<Mobile>();
			pathFinder = self.World.WorldActor.Trait<PathFinder>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			var ps1 = new PathSearch(self.World, mobile.Info, self)
			{
				checkForBlocked = true,
				heuristic = location => 0,
				inReverse = true
			};

			foreach (var cell in Util.AdjacentCells(target))
			{
				if (cell == self.Location)
					return NextActivity;
				else
					ps1.AddInitialCell(cell);
			}

			ps1.heuristic = PathSearch.DefaultEstimator(mobile.toCell);
			var ps2 = PathSearch.FromPoint(self.World, mobile.Info, self, mobile.toCell, target.CenterPosition.ToCPos(), true);
			var ret = pathFinder.FindBidiPath(ps1, ps2);

			return Util.SequenceActivities(mobile.MoveTo(() => ret), this);
		}
	}
}
