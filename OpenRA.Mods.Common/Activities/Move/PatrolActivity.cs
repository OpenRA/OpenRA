#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	public class PatrolActivity : Activity
	{
		readonly IMove move;
		readonly Patrol patrol;
		readonly int wait;
		readonly bool assaultMove;
		readonly Color? targetLineColor;

		int currentWaypoint = 0;
		short direction = 1;

		public PatrolActivity(IMove move, Patrol patrol, Color? targetLineColor, int wait = 0, bool assaultMove = false)
		{
			this.move = move;
			this.patrol = patrol;

			this.targetLineColor = targetLineColor;

			this.wait = wait;
			this.assaultMove = assaultMove;

			ChildHasPriority = false;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (!IsCanceling)
				patrol.AddStartingPoint(self.Location);
		}

		public override bool Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				TickChild(self);
				return false;
			}

			if (IsCanceling || patrol.PatrolWaypoints.Count < 2)
				return true;

			var wpt = GetNextWaypoint();
			QueueChild(new AttackMoveActivity(self, () => move.MoveTo(wpt, 2, targetLineColor: targetLineColor != null ? Color.OrangeRed : null), assaultMove));

			if (wait > 0)
				QueueChild(new Wait(wait));

			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (ChildActivity != null)
				foreach (var target in ChildActivity.GetTargets(self))
					yield return target;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor == null || patrol.PatrolWaypoints.Count == 0)
				yield break;

			if (ChildActivity != null)
				foreach (var node in ChildActivity.TargetLineNodes(self))
					yield return node;

			// Render attack line when attack activity is still being queued.
			else
				yield return new TargetLineNode(Target.FromCell(self.World, patrol.PatrolWaypoints[currentWaypoint]),
					State == ActivityState.Queued
					? targetLineColor.Value
					: Color.OrangeRed);

			foreach (var i in GetLineIndices(currentWaypoint, patrol.PatrolWaypoints.Count, direction))
				yield return new TargetLineNode(Target.FromCell(self.World, patrol.PatrolWaypoints[i]), targetLineColor.Value);
		}

		static IEnumerable<int> GetLineIndices(int index, int count, int direction)
		{
			if (count == 1)
			{
				yield return 0;
				yield break;
			}

			var i = index + direction;
			var dir = direction;
			while (i != index || dir != direction)
			{
				if (i < 0 || i >= count)
				{
					dir = -dir;
					i += dir;
				}
				else
				{
					yield return i;
					i += dir;
				}
			}
		}

		CPos GetNextWaypoint()
		{
			// Check if it's a ring.
			if (patrol.PatrolWaypoints[0] == patrol.PatrolWaypoints[^1])
			{
				currentWaypoint = (currentWaypoint + direction + patrol.PatrolWaypoints.Count) % patrol.PatrolWaypoints.Count;
				return patrol.PatrolWaypoints[currentWaypoint];
			}

			if (currentWaypoint + direction < 0 || currentWaypoint + direction >= patrol.PatrolWaypoints.Count)
				direction *= -1;

			currentWaypoint += direction;
			return patrol.PatrolWaypoints[currentWaypoint];
		}
	}
}
