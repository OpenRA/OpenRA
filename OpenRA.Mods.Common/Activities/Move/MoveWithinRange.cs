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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class MoveWithinRange : MoveAdjacentTo
	{
		readonly WDist maxRange;
		readonly WDist minRange;
		readonly Map map;
		readonly int maxCells;
		readonly int minCells;

		public MoveWithinRange(Actor self, in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
			: base(self, target, initialTargetPosition, targetLineColor)
		{
			this.minRange = minRange;
			this.maxRange = maxRange;
			map = self.World.Map;
			maxCells = (maxRange.Length + 1023) / 1024;
			minCells = minRange.Length / 1024;
		}

		protected override bool ShouldStop(Actor self)
		{
			// We are now in range. Don't move any further!
			// HACK: This works around the pathfinder not returning the shortest path
			return AtCorrectRange(self.CenterPosition) && Mobile.CanInteractWithGroundLayer(self) && Mobile.CanStayInCell(self.Location);
		}

		protected override bool ShouldRepath(Actor self, CPos targetLocation)
		{
			return lastVisibleTargetLocation != targetLocation && (!AtCorrectRange(self.CenterPosition)
				|| !Mobile.CanInteractWithGroundLayer(self) || !Mobile.CanStayInCell(self.Location));
		}

		protected override IEnumerable<CPos> CandidateMovementCells(Actor self)
		{
			return map.FindTilesInAnnulus(lastVisibleTargetLocation, minCells, maxCells)
				.Where(c => Mobile.CanStayInCell(c) && AtCorrectRange(map.CenterOfSubCell(c, Mobile.FromSubCell)));
		}

		bool AtCorrectRange(WPos origin)
		{
			return Target.IsInRange(origin, maxRange) && !Target.IsInRange(origin, minRange);
		}
	}
}
