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
		readonly DomainIndex domainIndex;
		readonly int movementClass;
		readonly WRange maxRange;
		readonly WRange minRange;

		Activity inner;
		CPos cachedTargetPosition;
		CPos[] targetCells;
		bool repath;

		public MoveAdjacentTo(Actor self, Target target)
		{
			this.target = target;

			mobile = self.Trait<Mobile>();
			pathFinder = self.World.WorldActor.Trait<PathFinder>();
			domainIndex = self.World.WorldActor.TraitOrDefault<DomainIndex>();
			movementClass = mobile.Info.GetMovementClass(self.World.TileSet);

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
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			var targetPosition = target.CenterPosition.ToCPos();

			// Calculate path to target
			if (inner == null && repath)
			{
				cachedTargetPosition = targetPosition;
				repath = false;

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
					}).ToArray();
				}
				else
					targetCells = Util.AdjacentCells(target).ToArray();


				var loc = self.Location;
				var searchCells = new List<CPos>();
				foreach (var cell in targetCells)
				{
					if (cell == loc)
						return NextActivity;
					else if (domainIndex == null || domainIndex.IsPassable(loc, cell, (uint)movementClass))
						searchCells.Add(cell);
				}

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
					var ps2 = PathSearch.FromPoint(self.World, mobile.Info, self, mobile.toCell, target.CenterPosition.ToCPos(), true);
					var ret = pathFinder.FindBidiPath(ps1, ps2);

					inner = mobile.MoveTo(() => ret);
				}
			}

			// Force a repath once the actor reaches the next cell
			if (!repath && cachedTargetPosition != targetPosition)
			{
				if (inner != null)
					inner.Cancel(self);

				repath = true;
			}

			inner = Util.RunActivity(self, inner);

			// Move completed
			if (inner == null && targetCells.Contains(self.Location))
				return NextActivity;

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (inner != null)
				return inner.GetTargets(self);

			return Target.None;
		}
	}
}
