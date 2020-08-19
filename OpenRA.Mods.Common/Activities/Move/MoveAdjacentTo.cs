#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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

		public MoveAdjacentTo(Actor self, in Target target, WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			this.target = target;
			this.targetLineColor = targetLineColor;
			Mobile = self.Trait<Mobile>();
			pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();
			ChildHasPriority = false;

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
			return Util.AdjacentCells(self.World, Target)
				.Where(c => Mobile.CanStayInCell(c));
		}

		protected override void OnFirstRun(Actor self)
		{
			QueueChild(Mobile.MoveTo(check => CalculatePathToTarget(self, check)));
		}

		public override bool Tick(Actor self)
		{
			var oldTargetLocation = lastVisibleTargetLocation;
			target = target.Recalculate(self.Owner, out var targetIsHiddenActor);
			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
			{
				lastVisibleTarget = Target.FromTargetPositions(target);
				lastVisibleTargetLocation = self.World.Map.CellContaining(target.CenterPosition);
			}

			// Target is equivalent to checkTarget variable in other activities
			// value is either lastVisibleTarget or target based on visibility and validity
			var targetIsValid = Target.IsValidFor(self);
			useLastVisibleTarget = targetIsHiddenActor || !targetIsValid;

			// Target is hidden or dead, and we don't have a fallback position to move towards
			var noTarget = useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self);

			// Cancel the current path if the activity asks to stop, or asks to repath
			// The repath happens once the move activity stops in the next cell
			var shouldRepath = targetIsValid && ShouldRepath(self, oldTargetLocation);
			if (ChildActivity != null && (ShouldStop(self) || shouldRepath || noTarget))
				ChildActivity.Cancel(self);

			// Target has moved, and MoveAdjacentTo is still valid.
			if (!IsCanceling && shouldRepath)
				QueueChild(Mobile.MoveTo(check => CalculatePathToTarget(self, check)));

			// The last queued childactivity is guaranteed to be the inner move, so if the childactivity
			// queue is empty it means we have reached our destination.
			return TickChild(self);
		}

		List<CPos> searchCells = new List<CPos>();
		int searchCellsTick = -1;

		List<CPos> CalculatePathToTarget(Actor self, BlockedByActor check)
		{
			var loc = self.Location;

			// PERF: Assume that CandidateMovementCells doesn't change within a tick to avoid repeated queries
			// when Move enumerates different BlockedByActor values
			if (searchCellsTick != self.World.WorldTick)
			{
				searchCells.Clear();
				searchCellsTick = self.World.WorldTick;
				foreach (var cell in CandidateMovementCells(self))
					if (domainIndex.IsPassable(loc, cell, Mobile.Locomotor) && Mobile.CanEnterCell(cell))
						searchCells.Add(cell);
			}

			if (!searchCells.Any())
				return NoPath;

			using (var fromSrc = PathSearch.FromPoints(self.World, Mobile.Locomotor, self, searchCells, loc, check))
			using (var fromDest = PathSearch.FromPoint(self.World, Mobile.Locomotor, self, loc, lastVisibleTargetLocation, check).Reverse())
				return pathFinder.FindBidiPath(fromSrc, fromDest);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (ChildActivity != null)
				return ChildActivity.GetTargets(self);

			return Target.None;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor.HasValue)
				yield return new TargetLineNode(useLastVisibleTarget ? lastVisibleTarget : target, targetLineColor.Value);
		}
	}
}
