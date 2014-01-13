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
		readonly uint movementClass;

		Activity inner;
		CPos targetPosition;
		bool repath;

		public MoveAdjacentTo(Actor self, Target target)
		{
			this.target = target;

			mobile = self.Trait<Mobile>();
			pathFinder = self.World.WorldActor.Trait<PathFinder>();
			domainIndex = self.World.WorldActor.TraitOrDefault<DomainIndex>();
			movementClass = (uint)mobile.Info.GetMovementClass(self.World.TileSet);

			repath = true;
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
				UpdateInnerPath(self);
				repath = false;
			}

			if (targetIsValid)
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
			var targetCells = Util.AdjacentCells(target);
			var searchCells = new List<CPos>();
			var loc = self.Location;

			foreach (var cell in targetCells)
				if (mobile.CanEnterCell(cell) && (domainIndex == null || domainIndex.IsPassable(loc, cell, movementClass)))
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
