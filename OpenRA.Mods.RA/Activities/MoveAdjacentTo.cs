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
	public class MoveAdjacentTo : Activity
	{
		readonly Target target;
		readonly Mobile mobile;
		readonly PathFinder pathFinder;
		readonly WRange maxRange;
		readonly WRange minRange;

		Activity inner;
		CPos targetPosition;
		CPos[] targetCells;
		bool repath;

		public MoveAdjacentTo(Actor self, Target target)
		{
			this.target = target;

			mobile = self.Trait<Mobile>();
			pathFinder = self.World.WorldActor.Trait<PathFinder>();

			repath = true;
		}

		public MoveAdjacentTo(Actor self, Target target, WRange minRange, WRange maxRange)
			: this(self, target)
		{
			this.minRange = minRange;
			this.maxRange = maxRange;
		}

		public override Activity Tick(Actor self)
		{
			if (inner == null)
			{
				// Inner move order has completed.
				if (repath && !IsCanceled && target.IsValidFor(self))
				{
					// Repath only if we cancelled inner because the target moved.
					UpdateInnerPath(self);
					repath = false;
				}
				else
				{
					// Otherweise, we're done here.
					return NextActivity;
				}
			}

			if (target.IsValidFor(self))
			{
				// Check if the target has moved
				var oldPosition = targetPosition;
				targetPosition = target.CenterPosition.ToCPos();
				if (!repath && targetPosition != oldPosition)
				{
					// Finish moving into the next cell and then repath.
					if (inner != null)
						inner.Cancel(self);
					repath = true;
				}
			}
			else
			{
				// Target became invalid. Cancel the inner order,
				// and then wait for it to move into the next cell
				// before finishing this order (handled above).
				inner.Cancel(self);
			}

			// Ticks the inner move activity to actually move the actor.
			inner = Util.RunActivity(self, inner);

			return this;
		}

		void UpdateInnerPath(Actor self)
		{
			if (maxRange != WRange.Zero)
			{
				// Move to a cell within the requested annulus
				var maxCells = (maxRange.Range + 1023) / 1024;
				var outerCells = self.World.FindTilesInCircle(targetPosition, maxCells);

				var minCells = minRange.Range / 1024;
				var innerCells = self.World.FindTilesInCircle(targetPosition, minCells);

				var outerSq = maxRange.Range * maxRange.Range;
				var innerSq = minRange.Range * minRange.Range;
				var center = targetPosition.CenterPosition;
				targetCells = outerCells.Except(innerCells).Where(c =>
				{
					var dxSq = (c.CenterPosition - center).HorizontalLengthSquared;
					return dxSq >= innerSq || dxSq <= outerSq;
				}).OrderByDescending(c => (c.CenterPosition - center).HorizontalLengthSquared).ToArray();
			}
			else
				targetCells = Util.AdjacentCells(target).ToArray();

			var searchCells = new List<CPos>();
			foreach (var cell in targetCells)
				if (mobile.CanEnterCell(cell))
					searchCells.Add(cell);

			if (searchCells.Any())
			{
				var ps1 = new PathSearch(self.World, mobile.Info, self)
				{
					checkForBlocked = true,
					heuristic = location => 0,
					inReverse = true
				};

				foreach (var cell in searchCells)
					ps1.AddInitialCell(cell);

				ps1.heuristic = PathSearch.DefaultEstimator(mobile.toCell);
				var ps2 = PathSearch.FromPoint(self.World, mobile.Info, self, mobile.toCell, targetPosition, true);
				var ret = pathFinder.FindBidiPath(ps1, ps2);

				inner = mobile.MoveTo(() => ret);
			}
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (inner != null)
				return inner.GetTargets(self);

			return Target.None;
		}

		public override void Cancel(Actor self)
		{
			if (inner != null)
				inner.Cancel(self);

			base.Cancel(self);
		}
	}
}
