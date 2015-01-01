#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class MoveAdjacentTo : Activity
	{
		static readonly List<CPos> NoPath = new List<CPos>();

		readonly Mobile mobile;
		readonly PathFinder pathFinder;
		readonly DomainIndex domainIndex;
		readonly uint movementClass;

		protected Target target { get; private set; }
		protected CPos targetPosition;
		Activity inner;
		bool repath;

		public MoveAdjacentTo(Actor self, Target target)
		{
			this.target = target;

			mobile = self.Trait<Mobile>();
			pathFinder = self.World.WorldActor.Trait<PathFinder>();
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();
			movementClass = (uint)mobile.Info.GetMovementClass(self.World.TileSet);

			if (target.IsValidFor(self))
				targetPosition = self.World.Map.CellContaining(target.CenterPosition);

			repath = true;
		}

		protected virtual bool ShouldStop(Actor self, CPos oldTargetPosition)
		{
			return false;
		}

		protected virtual bool ShouldRepath(Actor self, CPos oldTargetPosition)
		{
			return targetPosition != oldTargetPosition;
		}

		protected virtual IEnumerable<CPos> CandidateMovementCells(Actor self)
		{
			return Util.AdjacentCells(self.World, target);
		}

		public override Activity Tick(Actor self)
		{
			var targetIsValid = target.IsValidFor(self);

			// Inner move order has completed.
			if (inner == null)
			{
				// We are done here if the order was cancelled for any
				// reason except the target moving.
				if (IsCanceled || !repath || !targetIsValid)
					return NextActivity;

				// Target has moved, and MoveAdjacentTo is still valid.
				inner = mobile.MoveTo(() => CalculatePathToTarget(self));
				repath = false;
			}

			if (targetIsValid)
			{
				// Check if the target has moved
				var oldTargetPosition = targetPosition;
				targetPosition = self.World.Map.CellContaining(target.CenterPosition);

				var shouldStop = ShouldStop(self, oldTargetPosition);
				if (shouldStop || (!repath && ShouldRepath(self, oldTargetPosition)))
				{
					// Finish moving into the next cell and then repath.
					if (inner != null)
						inner.Cancel(self);

					repath = !shouldStop;
				}
			}
			else
			{
				// Target became invalid. Move to its last known position.
				target = Target.FromCell(self.World, targetPosition);
			}

			// Ticks the inner move activity to actually move the actor.
			inner = Util.RunActivity(self, inner);

			return this;
		}

		List<CPos> CalculatePathToTarget(Actor self)
		{
			var targetCells = CandidateMovementCells(self);
			var searchCells = new List<CPos>();
			var loc = self.Location;

			foreach (var cell in targetCells)
				if (domainIndex.IsPassable(loc, cell, movementClass) && mobile.CanEnterCell(cell))
					searchCells.Add(cell);

			if (!searchCells.Any())
				return NoPath;

			var fromSrc = PathSearch.FromPoints(self.World, mobile.Info, self, searchCells, loc, true);
			var fromDest = PathSearch.FromPoint(self.World, mobile.Info, self, loc, targetPosition, true).Reverse();

			return pathFinder.FindBidiPath(fromSrc, fromDest);
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
