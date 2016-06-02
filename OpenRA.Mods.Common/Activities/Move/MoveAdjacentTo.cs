#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class MoveAdjacentTo : Activity
	{
		static readonly List<CPos> NoPath = new List<CPos>();

		readonly Mobile mobile;
		readonly IPathFinder pathFinder;
		readonly DomainIndex domainIndex;
		readonly uint movementClass;

		Target target;
		bool canHideUnderFog;
		protected Target Target
		{
			get
			{
				return target;
			}

			private set
			{
				target = value;
				if (target.Type == TargetType.Actor)
					canHideUnderFog = target.Actor.Info.HasTraitInfo<HiddenUnderFogInfo>();
			}
		}

		protected CPos targetPosition;
		Activity inner;
		bool repath;

		public MoveAdjacentTo(Actor self, Target target)
		{
			Target = target;

			mobile = self.Trait<Mobile>();
			pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();
			movementClass = (uint)mobile.Info.GetMovementClass(self.World.Map.Rules.TileSet);

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
			return Util.AdjacentCells(self.World, Target);
		}

		public override Activity Tick(Actor self)
		{
			var targetIsValid = Target.IsValidFor(self);

			// Target moved under the fog. Move to its last known position.
			if (Target.Type == TargetType.Actor && canHideUnderFog
				&& !self.Owner.CanTargetActor(Target.Actor))
			{
				if (inner != null)
					inner.Cancel(self);

				self.SetTargetLine(Target.FromCell(self.World, targetPosition), Color.Green);
				return ActivityUtils.RunActivity(self, new AttackMoveActivity(self, mobile.MoveTo(targetPosition, 0)));
			}

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
				targetPosition = self.World.Map.CellContaining(Target.CenterPosition);

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
				Target = Target.FromCell(self.World, targetPosition);
			}

			// Ticks the inner move activity to actually move the actor.
			inner = ActivityUtils.RunActivity(self, inner);

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

			using (var fromSrc = PathSearch.FromPoints(self.World, mobile.Info, self, searchCells, loc, true))
			using (var fromDest = PathSearch.FromPoint(self.World, mobile.Info, self, loc, targetPosition, true).Reverse())
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
