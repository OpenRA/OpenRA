#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class MoveWithinRange : MoveAdjacentTo
	{
		readonly WDist maxRange;
		readonly WDist minRange;

		public MoveWithinRange(Actor self, Target target, WDist minRange, WDist maxRange)
			: base(self, target)
		{
			this.minRange = minRange;
			this.maxRange = maxRange;
		}

		protected override bool ShouldStop(Actor self, CPos oldTargetPosition)
		{
			// We are now in range. Don't move any further!
			// HACK: This works around the pathfinder not returning the shortest path
			return AtCorrectRange(self.CenterPosition) && Mobile.CanInteractWithGroundLayer(self);
		}

		protected override bool ShouldRepath(Actor self, CPos oldTargetPosition)
		{
			return targetPosition != oldTargetPosition && (!AtCorrectRange(self.CenterPosition)
				|| !Mobile.CanInteractWithGroundLayer(self));
		}

		protected override IEnumerable<CPos> CandidateMovementCells(Actor self)
		{
			var map = self.World.Map;
			var maxCells = (maxRange.Length + 1023) / 1024;
			var minCells = minRange.Length / 1024;

			if (minCells != 0 && Target.IsInRange(self.CenterPosition, minRange))
			{
				// Avoid cells on the far side of the enemy by requiring a negative dot product.
				return map.FindTilesInAnnulus(targetPosition, minCells, maxCells)
					.Where(c => AtCorrectRange(map.CenterOfSubCell(c, Mobile.FromSubCell))
						&& CVec.Dot(c - self.Location, targetPosition - self.Location) < 0);
			}

			return map.FindTilesInAnnulus(targetPosition, minCells, maxCells)
				.Where(c => AtCorrectRange(map.CenterOfSubCell(c, Mobile.FromSubCell)));
		}

		bool AtCorrectRange(WPos origin)
		{
			return Target.IsInRange(origin, maxRange) && !Target.IsInRange(origin, minRange);
		}
	}
}
