#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class MoveWithinRange : MoveAdjacentTo
	{
		readonly WRange maxRange;
		readonly WRange minRange;

		public MoveWithinRange(Actor self, Target target, WRange minRange, WRange maxRange)
			: base(self, target)
		{
			this.minRange = minRange;
			this.maxRange = maxRange;
		}

		protected override bool ShouldStop(Actor self, CPos oldTargetPosition)
		{
			// We are now in range. Don't move any further!
			// HACK: This works around the pathfinder not returning the shortest path
			var cp = self.CenterPosition;
			return target.IsInRange(cp, maxRange) && !target.IsInRange(cp, minRange);
		}

		protected override bool ShouldRepath(Actor self, CPos oldTargetPosition)
		{
			var cp = self.CenterPosition;
			return targetPosition != oldTargetPosition && (!target.IsInRange(cp, maxRange) || target.IsInRange(cp, minRange));
		}

		protected override IEnumerable<CPos> CandidateMovementCells(Actor self)
		{
			var map = self.World.Map;
			var maxCells = (maxRange.Range + 1023) / 1024;
			var minCells = minRange.Range / 1024;

			var outerSq = maxRange.Range * maxRange.Range;
			var innerSq = minRange.Range * minRange.Range;
			var center = target.CenterPosition;

			return map.FindTilesInAnnulus(targetPosition, minCells + 1, maxCells).Where(c =>
			{
				var dxSq = (map.CenterOfCell(c) - center).HorizontalLengthSquared;
				return dxSq >= innerSq && dxSq <= outerSq;
			});
		}
	}
}
