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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Move : Activity
	{
		static readonly List<CPos> NoPath = new List<CPos>();

		readonly Mobile mobile;
		readonly WDist nearEnough;
		readonly Func<BlockedByActor, List<CPos>> getPath;
		readonly Actor ignoreActor;
		readonly Color? targetLineColor;

		List<CPos> path;
		CPos? destination;

		// For dealing with blockers
		bool hasWaited;
		int waitTicksRemaining;

		// To work around queued activity issues while minimizing changes to legacy behaviour
		bool evaluateNearestMovableCell;

		// Scriptable move order
		// Ignores lane bias and nearby units
		public Move(Actor self, CPos destination, Color? targetLineColor = null)
		{
			mobile = self.Trait<Mobile>();

			getPath = check =>
			{
				List<CPos> path;
				using (var search =
					PathSearch.FromPoint(self.World, mobile.Locomotor, self, mobile.ToCell, destination, check)
					.WithoutLaneBias())
					path = self.World.WorldActor.Trait<IPathFinder>().FindPath(search);
				return path;
			};
			this.destination = destination;
			this.targetLineColor = targetLineColor;
			nearEnough = WDist.Zero;
		}

		public Move(Actor self, CPos destination, WDist nearEnough, Actor ignoreActor = null, bool evaluateNearestMovableCell = false,
			Color? targetLineColor = null)
		{
			mobile = self.Trait<Mobile>();

			getPath = check =>
			{
				if (!this.destination.HasValue)
					return NoPath;

				return self.World.WorldActor.Trait<IPathFinder>()
					.FindUnitPath(mobile.ToCell, this.destination.Value, self, ignoreActor, check);
			};

			// Note: Will be recalculated from OnFirstRun if evaluateNearestMovableCell is true
			this.destination = destination;

			this.nearEnough = nearEnough;
			this.ignoreActor = ignoreActor;
			this.evaluateNearestMovableCell = evaluateNearestMovableCell;
			this.targetLineColor = targetLineColor;
		}

		public Move(Actor self, CPos destination, SubCell subCell, WDist nearEnough, Color? targetLineColor = null)
		{
			mobile = self.Trait<Mobile>();

			getPath = check => self.World.WorldActor.Trait<IPathFinder>()
				.FindUnitPathToRange(mobile.FromCell, subCell, self.World.Map.CenterOfSubCell(destination, subCell), nearEnough, self, check);
			this.destination = destination;
			this.nearEnough = nearEnough;
			this.targetLineColor = targetLineColor;
		}

		public Move(Actor self, Target target, WDist range, Color? targetLineColor = null)
		{
			mobile = self.Trait<Mobile>();

			getPath = check =>
			{
				if (!target.IsValidFor(self))
					return NoPath;

				return self.World.WorldActor.Trait<IPathFinder>().FindUnitPathToRange(
					mobile.ToCell, mobile.ToSubCell, target.CenterPosition, range, self, check);
			};

			destination = null;
			nearEnough = range;
			this.targetLineColor = targetLineColor;
		}

		public Move(Actor self, Func<BlockedByActor, List<CPos>> getPath, Color? targetLineColor = null)
		{
			mobile = self.Trait<Mobile>();

			this.getPath = getPath;

			destination = null;
			nearEnough = WDist.Zero;
			this.targetLineColor = targetLineColor;
		}

		static int HashList<T>(List<T> xs)
		{
			var hash = 0;
			var n = 0;
			foreach (var x in xs)
				hash += n++ * x.GetHashCode();

			return hash;
		}

		List<CPos> EvalPath(BlockedByActor check)
		{
			var path = getPath(check).TakeWhile(a => a != mobile.ToCell).ToList();
			mobile.PathHash = HashList(path);
			return path;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (evaluateNearestMovableCell && destination.HasValue)
			{
				var movableDestination = mobile.NearestMoveableCell(destination.Value);
				destination = mobile.CanEnterCell(movableDestination) ? movableDestination : (CPos?)null;
			}

			path = EvalPath(BlockedByActor.Stationary);
			if (path.Count == 0)
				path = EvalPath(BlockedByActor.None);
		}

		public override bool Tick(Actor self)
		{
			mobile.TurnToMove = false;

			// If the actor is inside a tunnel then we must let them move
			// all the way through before moving to the next activity
			if (IsCanceling && self.Location.Layer != CustomMovementLayerType.Tunnel)
				return true;

			if (mobile.IsTraitDisabled || mobile.IsTraitPaused)
				return false;

			if (destination == mobile.ToCell)
				return true;

			if (path.Count == 0)
			{
				destination = mobile.ToCell;
				return false;
			}

			destination = path[0];

			var nextCell = PopPath(self);
			if (nextCell == null)
				return false;

			var firstFacing = self.World.Map.FacingBetween(mobile.FromCell, nextCell.Value.First, mobile.Facing);
			if (firstFacing != mobile.Facing)
			{
				path.Add(nextCell.Value.First);
				QueueChild(new Turn(self, firstFacing));
				mobile.TurnToMove = true;
				return false;
			}

			mobile.SetLocation(mobile.FromCell, mobile.FromSubCell, nextCell.Value.First, nextCell.Value.Second);

			var map = self.World.Map;
			var from = (mobile.FromCell.Layer == 0 ? map.CenterOfCell(mobile.FromCell) :
				self.World.GetCustomMovementLayers()[mobile.FromCell.Layer].CenterOfCell(mobile.FromCell)) +
				map.Grid.OffsetOfSubCell(mobile.FromSubCell);

			var to = Util.BetweenCells(self.World, mobile.FromCell, mobile.ToCell) +
				(map.Grid.OffsetOfSubCell(mobile.FromSubCell) + map.Grid.OffsetOfSubCell(mobile.ToSubCell)) / 2;

			QueueChild(new MoveFirstHalf(this, from, to, mobile.Facing, mobile.Facing, 0));
			return false;
		}

		Pair<CPos, SubCell>? PopPath(Actor self)
		{
			if (path.Count == 0)
				return null;

			var nextCell = path[path.Count - 1];

			var containsTemporaryBlocker = WorldUtils.ContainsTemporaryBlocker(self.World, nextCell, self);

			// Next cell in the move is blocked by another actor
			if (containsTemporaryBlocker || !mobile.CanEnterCell(nextCell, ignoreActor))
			{
				// Are we close enough?
				var cellRange = nearEnough.Length / 1024;
				if (!containsTemporaryBlocker && (mobile.ToCell - destination.Value).LengthSquared <= cellRange * cellRange)
				{
					path.Clear();
					return null;
				}

				// There is no point in waiting for the other actor to move if it is incapable of moving.
				if (!mobile.CanEnterCell(nextCell, ignoreActor, BlockedByActor.Immovable))
				{
					path = EvalPath(BlockedByActor.Immovable);
					return null;
				}

				// See if they will move
				self.NotifyBlocker(nextCell);

				// Wait a bit to see if they leave
				if (!hasWaited)
				{
					waitTicksRemaining = mobile.Info.LocomotorInfo.WaitAverage;
					hasWaited = true;
					return null;
				}

				if (--waitTicksRemaining >= 0)
					return null;

				hasWaited = false;

				// If the blocking actors are already leaving, wait a little longer instead of repathing
				if (CellIsEvacuating(self, nextCell))
					return null;

				// Calculate a new path
				mobile.RemoveInfluence();
				var newPath = EvalPath(BlockedByActor.All);
				mobile.AddInfluence();

				if (newPath.Count != 0)
				{
					path = newPath;
					var newCell = path[path.Count - 1];
					path.RemoveAt(path.Count - 1);

					return Pair.New(newCell, mobile.GetAvailableSubCell(nextCell, SubCell.Any, ignoreActor));
				}
				else if (mobile.IsBlocking)
				{
					// If there is no way around the blocker and blocker will not move and we are blocking others, back up to let others pass.
					var newCell = GetAdjacentCell(self, nextCell);
					if (newCell != null)
					{
						if ((nextCell - newCell).Value.LengthSquared > 2)
							path.Add(mobile.ToCell);

						return Pair.New(newCell.Value, mobile.GetAvailableSubCell(newCell.Value, SubCell.Any, ignoreActor));
					}
				}

				return null;
			}

			hasWaited = false;
			path.RemoveAt(path.Count - 1);

			return Pair.New(nextCell, mobile.GetAvailableSubCell(nextCell, SubCell.Any, ignoreActor));
		}

		protected override void OnLastRun(Actor self)
		{
			path = null;
		}

		bool CellIsEvacuating(Actor self, CPos cell)
		{
			foreach (var actor in self.World.ActorMap.GetActorsAt(cell))
			{
				var move = actor.TraitOrDefault<Mobile>();
				if (move == null || !move.IsTraitEnabled() || !move.IsLeaving())
					return false;
			}

			return true;
		}

		CPos? GetAdjacentCell(Actor self, CPos nextCell)
		{
			var availCells = new List<CPos>();
			var notStupidCells = new List<CPos>();
			for (var i = -1; i <= 1; i++)
			{
				for (var j = -1; j <= 1; j++)
				{
					var p = mobile.ToCell + new CVec(i, j);
					if (mobile.CanEnterCell(p))
						availCells.Add(p);
					else if (p != nextCell && p != mobile.ToCell)
						notStupidCells.Add(p);
				}
			}

			CPos? newCell = null;
			if (availCells.Count > 0)
				newCell = availCells.Random(self.World.SharedRandom);
			else
			{
				var cellInfo = notStupidCells
					.SelectMany(c => self.World.ActorMap.GetActorsAt(c)
						.Where(a => a.IsIdle && a.Info.HasTraitInfo<MobileInfo>()),
						(c, a) => new { Cell = c, Actor = a })
					.RandomOrDefault(self.World.SharedRandom);
				if (cellInfo != null)
					newCell = cellInfo.Cell;
			}

			return newCell;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			if (path != null)
				path.Clear();

			base.Cancel(self, keepQueue);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (path != null)
				return Enumerable.Reverse(path).Select(c => Target.FromCell(self.World, c));
			if (destination != null)
				return new Target[] { Target.FromCell(self.World, destination.Value) };
			return Target.None;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor != null)
				yield return new TargetLineNode(Target.FromCell(self.World, destination.Value), targetLineColor.Value);
		}

		abstract class MovePart : Activity
		{
			protected readonly Move Move;
			protected readonly WPos From, To;
			protected readonly int FromFacing, ToFacing;
			protected readonly bool EnableArc;
			protected readonly WPos ArcCenter;
			protected readonly int ArcFromLength;
			protected readonly WAngle ArcFromAngle;
			protected readonly int ArcToLength;
			protected readonly WAngle ArcToAngle;

			protected readonly int MoveFractionTotal;
			protected int moveFraction;

			public MovePart(Move move, WPos from, WPos to, int fromFacing, int toFacing, int startingFraction)
			{
				Move = move;
				From = from;
				To = to;
				FromFacing = fromFacing;
				ToFacing = toFacing;
				moveFraction = startingFraction;
				MoveFractionTotal = (to - from).Length;
				IsInterruptible = false; // See comments in Move.Cancel()

				// Calculate an elliptical arc that joins from and to
				var delta = Util.NormalizeFacing(fromFacing - toFacing);
				if (delta != 0 && delta != 128)
				{
					// The center of rotation is where the normal vectors cross
					var u = new WVec(1024, 0, 0).Rotate(WRot.FromFacing(fromFacing));
					var v = new WVec(1024, 0, 0).Rotate(WRot.FromFacing(toFacing));
					var w = from - to;
					var s = (v.Y * w.X - v.X * w.Y) * 1024 / (v.X * u.Y - v.Y * u.X);
					var x = from.X + s * u.X / 1024;
					var y = from.Y + s * u.Y / 1024;

					ArcCenter = new WPos(x, y, 0);
					ArcFromLength = (ArcCenter - from).HorizontalLength;
					ArcFromAngle = (ArcCenter - from).Yaw;
					ArcToLength = (ArcCenter - to).HorizontalLength;
					ArcToAngle = (ArcCenter - to).Yaw;
					EnableArc = true;
				}
			}

			public override bool Tick(Actor self)
			{
				if (Move.mobile.IsTraitDisabled)
					return false;

				var ret = InnerTick(self, Move.mobile);

				if (moveFraction > MoveFractionTotal)
					moveFraction = MoveFractionTotal;

				UpdateCenterLocation(self, Move.mobile);

				if (ret == this)
					return false;

				Queue(ret);
				return true;
			}

			Activity InnerTick(Actor self, Mobile mobile)
			{
				moveFraction += mobile.MovementSpeedForCell(self, mobile.ToCell);
				if (moveFraction <= MoveFractionTotal)
					return this;

				return OnComplete(self, mobile, Move);
			}

			void UpdateCenterLocation(Actor self, Mobile mobile)
			{
				// Avoid division through zero
				if (MoveFractionTotal != 0)
				{
					WPos pos;
					if (EnableArc)
					{
						var angle = WAngle.Lerp(ArcFromAngle, ArcToAngle, moveFraction, MoveFractionTotal);
						var length = int2.Lerp(ArcFromLength, ArcToLength, moveFraction, MoveFractionTotal);
						var height = int2.Lerp(From.Z, To.Z, moveFraction, MoveFractionTotal);
						pos = ArcCenter + new WVec(0, length, height).Rotate(WRot.FromYaw(angle));
					}
					else
						pos = WPos.Lerp(From, To, moveFraction, MoveFractionTotal);

					mobile.SetVisualPosition(self, pos);
				}
				else
					mobile.SetVisualPosition(self, To);

				if (moveFraction >= MoveFractionTotal)
					mobile.Facing = ToFacing & 0xFF;
				else
					mobile.Facing = int2.Lerp(FromFacing, ToFacing, moveFraction, MoveFractionTotal) & 0xFF;
			}

			protected abstract MovePart OnComplete(Actor self, Mobile mobile, Move parent);

			public override IEnumerable<Target> GetTargets(Actor self)
			{
				return Move.GetTargets(self);
			}
		}

		class MoveFirstHalf : MovePart
		{
			public MoveFirstHalf(Move move, WPos from, WPos to, int fromFacing, int toFacing, int startingFraction)
				: base(move, from, to, fromFacing, toFacing, startingFraction) { }

			static bool IsTurn(Mobile mobile, CPos nextCell, Map map)
			{
				// Tight U-turns should be done in place instead of making silly looking loops.
				var nextFacing = map.FacingBetween(nextCell, mobile.ToCell, mobile.Facing);
				var currentFacing = map.FacingBetween(mobile.ToCell, mobile.FromCell, mobile.Facing);
				var delta = Util.NormalizeFacing(nextFacing - currentFacing);
				return delta != 0 && (delta < 96 || delta > 160);
			}

			protected override MovePart OnComplete(Actor self, Mobile mobile, Move parent)
			{
				var map = self.World.Map;
				var fromSubcellOffset = map.Grid.OffsetOfSubCell(mobile.FromSubCell);
				var toSubcellOffset = map.Grid.OffsetOfSubCell(mobile.ToSubCell);

				var nextCell = parent.PopPath(self);
				if (nextCell != null)
				{
					if (IsTurn(mobile, nextCell.Value.First, map))
					{
						var nextSubcellOffset = map.Grid.OffsetOfSubCell(nextCell.Value.Second);
						var ret = new MoveFirstHalf(
							Move,
							Util.BetweenCells(self.World, mobile.FromCell, mobile.ToCell) + (fromSubcellOffset + toSubcellOffset) / 2,
							Util.BetweenCells(self.World, mobile.ToCell, nextCell.Value.First) + (toSubcellOffset + nextSubcellOffset) / 2,
							mobile.Facing,
							Util.GetNearestFacing(mobile.Facing, map.FacingBetween(mobile.ToCell, nextCell.Value.First, mobile.Facing)),
							moveFraction - MoveFractionTotal);

						mobile.FinishedMoving(self);
						mobile.SetLocation(mobile.ToCell, mobile.ToSubCell, nextCell.Value.First, nextCell.Value.Second);
						return ret;
					}

					parent.path.Add(nextCell.Value.First);
				}

				var toPos = mobile.ToCell.Layer == 0 ? map.CenterOfCell(mobile.ToCell) :
					self.World.GetCustomMovementLayers()[mobile.ToCell.Layer].CenterOfCell(mobile.ToCell);

				var ret2 = new MoveSecondHalf(
					Move,
					Util.BetweenCells(self.World, mobile.FromCell, mobile.ToCell) + (fromSubcellOffset + toSubcellOffset) / 2,
					toPos + toSubcellOffset,
					mobile.Facing,
					mobile.Facing,
					moveFraction - MoveFractionTotal);

				mobile.EnteringCell(self);
				mobile.SetLocation(mobile.ToCell, mobile.ToSubCell, mobile.ToCell, mobile.ToSubCell);
				return ret2;
			}
		}

		class MoveSecondHalf : MovePart
		{
			public MoveSecondHalf(Move move, WPos from, WPos to, int fromFacing, int toFacing, int startingFraction)
				: base(move, from, to, fromFacing, toFacing, startingFraction) { }

			protected override MovePart OnComplete(Actor self, Mobile mobile, Move parent)
			{
				mobile.SetPosition(self, mobile.ToCell);
				return null;
			}
		}
	}
}
