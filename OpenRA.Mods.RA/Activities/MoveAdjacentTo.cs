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

		public MoveAdjacentTo(Target target) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			var mobile = self.Trait<Mobile>();

			var sourcePath = new PathSearch(self.World, mobile.Info, self)
			{
				checkForBlocked = true,
				heuristic = location => 0,
				inReverse = true
			};

			foreach (var cell in Util.AdjacentCells(target))
			{
				if (cell == self.Location)
					return NextActivity;
				else if (mobile.Info.CanEnterCell(self.World, cell))
					sourcePath.AddInitialCell(cell);
			}

			if (sourcePath.queue.Empty)
				return NextActivity;

			sourcePath.heuristic = PathSearch.DefaultEstimator(mobile.toCell);
			var destPath = PathSearch.FromPoint(self.World, mobile.Info, self, mobile.toCell, target.CenterPosition.ToCPos(), true); // TODO: re-evaluate when target changes position
			var ret = self.World.WorldActor.Trait<PathFinder>().FindBidiPath(sourcePath, destPath);

			return Util.SequenceActivities(mobile.MoveTo(() => ret), this);
		}
	}
}
