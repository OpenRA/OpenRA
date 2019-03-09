#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class MoveAdjacentTo : Activity
	{
		static readonly List<CPos> NoPath = new List<CPos>();

		protected readonly Mobile Mobile;
		readonly IPathFinder pathFinder;
		readonly DomainIndex domainIndex;
		readonly Color? targetLineColor;

		protected Target Target
		{
			get
			{
				return useLastVisibleTarget ? lastVisibleTarget : target;
			}
		}

		Target target;
		Target lastVisibleTarget;
		protected CPos lastVisibleTargetLocation;
		bool useLastVisibleTarget;
		bool repath;

		public MoveAdjacentTo(Actor self, Target target, WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			this.target = target;
			this.targetLineColor = targetLineColor;
			Mobile = self.Trait<Mobile>();
			pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();

			// The target may become hidden between the initial order request and the first tick (e.g. if queued)
			// Moving to any position (even if quite stale) is still better than immediately giving up
			if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
			    || target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
			{
				lastVisibleTarget = Target.FromPos(target.CenterPosition);
				lastVisibleTargetLocation = self.World.Map.CellContaining(target.CenterPosition);
			}
			else if (initialTargetPosition.HasValue)
			{
				lastVisibleTarget = Target.FromPos(initialTargetPosition.Value);
				lastVisibleTargetLocation = self.World.Map.CellContaining(initialTargetPosition.Value);
			}

			repath = true;
		}

		protected virtual bool ShouldStop(Actor self)
		{
			return false;
		}

		protected virtual bool ShouldRepath(Actor self, CPos targetLocation)
		{
			return lastVisibleTargetLocation != targetLocation;
		}

		protected virtual IEnumerable<CPos> CandidateMovementCells(Actor self)
		{
			return Util.AdjacentCells(self.World, Target);
		}

		public override Activity Tick(Actor self)
		{
			bool targetIsHiddenActor;
			var oldTargetLocation = lastVisibleTargetLocation;
			target = target.Recalculate(self.Owner, out targetIsHiddenActor);
			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
			{
				lastVisibleTarget = Target.FromTargetPositions(target);
				lastVisibleTargetLocation = self.World.Map.CellContaining(target.CenterPosition);
			}

			// Target is equivalent to checkTarget variable in other activities
			// value is either lastVisibleTarget or target based on visibility and validity
			var targetIsValid = Target.IsValidFor(self);
			var oldUseLastVisibleTarget = useLastVisibleTarget;
			useLastVisibleTarget = targetIsHiddenActor || !targetIsValid;

			// Update target lines if required
			if (useLastVisibleTarget != oldUseLastVisibleTarget && targetLineColor.HasValue)
				self.SetTargetLine(useLastVisibleTarget ? lastVisibleTarget : target, targetLineColor.Value, false);

			// Target is hidden or dead, and we don't have a fallback position to move towards
			var noTarget = useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self);

			// Inner move order has completed.
			if (ChildActivity == null)
			{
				// We are done here if the order was cancelled for any
				// reason except the target moving.
				if (IsCanceling || !repath || !targetIsValid)
					return NextActivity;

				// Target has moved, and MoveAdjacentTo is still valid.
				ChildActivity = Mobile.MoveTo(() => CalculatePathToTarget(self));
				repath = false;
			}

			// Cancel the current path if the activity asks to stop, or asks to repath
			// The repath happens once the move activity stops in the next cell
			var shouldStop = ShouldStop(self);
			var shouldRepath = targetIsValid && !repath && ShouldRepath(self, oldTargetLocation);
			if (shouldStop || shouldRepath || noTarget)
			{
				if (ChildActivity != null)
					ChildActivity.Cancel(self);

				repath = shouldRepath;
			}

			// Ticks the inner move activity to actually move the actor.
			ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);

			return this;
		}

		List<CPos> CalculatePathToTarget(Actor self)
		{
			var targetCells = CandidateMovementCells(self);
			var searchCells = new List<CPos>();
			var loc = self.Location;

			foreach (var cell in targetCells)
				if (domainIndex.IsPassable(loc, cell, Mobile.Info.LocomotorInfo) && Mobile.CanEnterCell(cell))
					searchCells.Add(cell);

			if (!searchCells.Any())
				return NoPath;

			using (var fromSrc = PathSearch.FromPoints(self.World, Mobile.Info.LocomotorInfo, self, searchCells, loc, true))
			using (var fromDest = PathSearch.FromPoint(self.World, Mobile.Info.LocomotorInfo, self, loc, lastVisibleTargetLocation, true).Reverse())
				return pathFinder.FindBidiPath(fromSrc, fromDest);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (ChildActivity != null)
				return ChildActivity.GetTargets(self);

			return Target.None;
		}
	}
}
