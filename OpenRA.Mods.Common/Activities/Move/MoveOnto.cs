#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class MoveOnto : MoveAdjacentTo
	{
		readonly WVec offset = WVec.Zero;

		public MoveOnto(Actor self, in Target target, WVec? offset = null, WPos? initialTargetPosition = null, Color? targetLineColor = null)
			: base(self, target, initialTargetPosition, targetLineColor)
		{
			if (offset.HasValue)
				this.offset = offset.Value;
		}

		protected override void SetVisibleTargetLocation(Actor self, Target target)
		{
			lastVisibleTargetLocation = self.World.Map.CellContaining(Target.CenterPosition + offset);
		}

		protected override bool ShouldStop(Actor self)
		{
			// Stop if the target is dead.
			return Target.Type == TargetType.Terrain;
		}

		protected override List<CPos> CalculatePathToTarget(Actor self, BlockedByActor check)
		{
			// If we are close to the target but can't enter, we wait.
			if (!Mobile.CanEnterCell(lastVisibleTargetLocation) && Util.AreAdjacentCells(lastVisibleTargetLocation, self.Location))
				return PathFinder.NoPath;

			// PERF: Don't create a new list every run.
			// PERF: Also reuse the already created list in the base class.
			if (SearchCells.Count == 0)
				SearchCells.Add(lastVisibleTargetLocation);
			else if (SearchCells[0] != lastVisibleTargetLocation)
				SearchCells[0] = lastVisibleTargetLocation;

			return Mobile.PathFinder.FindPathToTargetCells(self, self.Location, SearchCells, check);
		}
	}
}
