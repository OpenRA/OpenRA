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
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Follow : Activity
	{
		readonly WDist minRange;
		readonly WDist maxRange;
		readonly IMove move;
		readonly Color? targetLineColor;
		Target target;
		Target lastVisibleTarget;
		bool useLastVisibleTarget;
		bool wasMovingWithinRange;

		public Follow(Actor self, in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition, Color? targetLineColor = null)
		{
			this.target = target;
			this.minRange = minRange;
			this.maxRange = maxRange;
			this.targetLineColor = targetLineColor;
			move = self.Trait<IMove>();

			// The target may become hidden between the initial order request and the first tick (e.g. if queued)
			// Moving to any position (even if quite stale) is still better than immediately giving up
			if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
			    || target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
				lastVisibleTarget = Target.FromPos(target.CenterPosition);
			else if (initialTargetPosition.HasValue)
				lastVisibleTarget = Target.FromPos(initialTargetPosition.Value);
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			target = target.Recalculate(self.Owner, out var targetIsHiddenActor);
			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
				lastVisibleTarget = Target.FromTargetPositions(target);

			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// If we are ticking again after previously sequencing a MoveWithRange then that move must have completed
			// Either we are in range and can see the target, or we've lost track of it and should give up
			if (wasMovingWithinRange && targetIsHiddenActor)
				return true;

			wasMovingWithinRange = false;

			// Target is hidden or dead, and we don't have a fallback position to move towards
			if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
				return true;

			var pos = self.CenterPosition;
			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

			// We've reached the required range - if the target is visible and valid then we wait
			// otherwise if it is hidden or dead we give up
			if (checkTarget.IsInRange(pos, maxRange) && !checkTarget.IsInRange(pos, minRange))
				return useLastVisibleTarget;

			// Move into range
			wasMovingWithinRange = true;
			QueueChild(move.MoveWithinRange(target, minRange, maxRange, checkTarget.CenterPosition, targetLineColor));
			return false;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor != null)
				yield return new TargetLineNode(useLastVisibleTarget ? lastVisibleTarget : target, targetLineColor.Value);
		}
	}
}
