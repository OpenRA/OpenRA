#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
		readonly Func<List<CPos>> getPath;
		readonly Actor ignoreActor;

		List<CPos> path;
		CPos? destination;

		// For dealing with blockers
		bool hasWaited;
		bool hasNotifiedBlocker;
		int waitTicksRemaining;

		// Scriptable move order
		// Ignores lane bias and nearby units
		public Move(Actor self, CPos destination)
		{
			mobile = self.Trait<Mobile>();

			getPath = () =>
			{
				List<CPos> path;
				using (var search =
					PathSearch.FromPoint(self.World, mobile.Info, self, mobile.ToCell, destination, false)
					.WithoutLaneBias())
					path = self.World.WorldActor.Trait<IPathFinder>().FindPath(search);
				return path;
			};
			this.destination = destination;
			nearEnough = WDist.Zero;
		}

		public Move(Actor self, CPos destination, WDist nearEnough, Actor ignoreActor = null)
		{
			mobile = self.Trait<Mobile>();

			getPath = () => self.World.WorldActor.Trait<IPathFinder>()
				.FindUnitPath(mobile.ToCell, destination, self, ignoreActor);
			this.destination = destination;
			this.nearEnough = nearEnough;
			this.ignoreActor = ignoreActor;
		}

		public Move(Actor self, CPos destination, SubCell subCell, WDist nearEnough)
		{
			mobile = self.Trait<Mobile>();

			getPath = () => self.World.WorldActor.Trait<IPathFinder>()
				.FindUnitPathToRange(mobile.FromCell, subCell, self.World.Map.CenterOfSubCell(destination, subCell), nearEnough, self);
			this.destination = destination;
			this.nearEnough = nearEnough;
		}

		public Move(Actor self, Target target, WDist range)
		{
			mobile = self.Trait<Mobile>();

			getPath = () =>
			{
				if (!target.IsValidFor(self))
					return NoPath;

				return self.World.WorldActor.Trait<IPathFinder>().FindUnitPathToRange(
					mobile.ToCell, mobile.ToSubCell, target.CenterPosition, range, self);
			};

			destination = null;
			nearEnough = range;
		}

		public Move(Actor self, Func<List<CPos>> getPath)
		{
			mobile = self.Trait<Mobile>();

			this.getPath = getPath;

			destination = null;
			nearEnough = WDist.Zero;
		}

		static int HashList<T>(List<T> xs)
		{
			var hash = 0;
			var n = 0;
			foreach (var x in xs)
				hash += n++ * x.GetHashCode();

			return hash;
		}

		List<CPos> EvalPath()
		{
			var path = getPath().TakeWhile(a => a != mobile.ToCell).ToList();
			mobile.PathHash = HashList(path);
			return path;
		}

		public override Activity Tick(Actor self)
		{
			// If the actor is inside a tunnel then we must let them move
			// all the way through before moving to the next activity
			if (IsCanceled && self.Location.Layer != CustomMovementLayerType.Tunnel)
				return NextActivity;

			if (mobile.IsTraitDisabled)
				return this;

			if (destination == mobile.ToCell)
				return NextActivity;

			if (path == null)
			{
				if (mobile.TicksBeforePathing > 0)
				{
					--mobile.TicksBeforePathing;
					return this;
				}

				path = EvalPath();
				SanityCheckPath(mobile);
			}

			if (path.Count == 0)
			{
				destination = mobile.ToCell;
				return this;
			}

			destination = path[0];

			var nextCell = PopPath(self);
			if (nextCell == null)
				return this;

			var firstFacing = self.World.Map.FacingBetween(mobile.FromCell, nextCell.Value.First, mobile.Facing);
			if (firstFacing != mobile.Facing)
			{
				path.Add(nextCell.Value.First);
				return ActivityUtils.SequenceActivities(new Turn(self, firstFacing), this);
			}
			else
			{
				mobile.SetLocation(mobile.FromCell, mobile.FromSubCell, nextCell.Value.First, nextCell.Value.Second);

				var map = self.World.Map;
				var from = (mobile.FromCell.Layer == 0 ? map.CenterOfCell(mobile.FromCell) :
					self.World.GetCustomMovementLayers()[mobile.FromCell.Layer].CenterOfCell(mobile.FromCell)) +
					map.Grid.OffsetOfSubCell(mobile.FromSubCell);

				var to = Util.BetweenCells(self.World, mobile.FromCell, mobile.ToCell) +
					(map.Grid.OffsetOfSubCell(mobile.FromSubCell) + map.Grid.OffsetOfSubCell(mobile.ToSubCell)) / 2;

				var move = new MoveFirstHalf(
					this,
					from,
					to,
					mobile.Facing,
					mobile.Facing,
					0);
				ChildActivity = move;

				return ChildActivity;
			}
		}

		[Conditional("SANITY_CHECKS")]
		void SanityCheckPath(Mobile mobile)
		{
			if (path.Count == 0)
				return;
			var d = path[path.Count - 1] - mobile.ToCell;
			if (d.LengthSquared > 2)
				throw new InvalidOperationException("(Move) Sanity check failed");
		}

		Pair<CPos, SubCell>? PopPath(Actor self)
		{
			if (path.Count == 0)
				return null;

			var nextCell = path[path.Count - 1];

			var containsTemporaryBlocker = WorldUtils.ContainsTemporaryBlocker(self.World, nextCell, self);

			// Next cell in the move is blocked by another actor
			if (containsTemporaryBlocker || !mobile.CanEnterCell(nextCell, ignoreActor, true))
			{
				// Are we close enough?
				var cellRange = nearEnough.Length / 1024;
				if (!containsTemporaryBlocker && (mobile.ToCell - destination.Value).LengthSquared <= cellRange * cellRange)
				{
					path.Clear();
					return null;
				}

				// See if they will move
				if (!hasNotifiedBlocker)
				{
					self.NotifyBlocker(nextCell);
					hasNotifiedBlocker = true;
				}

				// Wait a bit to see if they leave
				if (!hasWaited)
				{
					waitTicksRemaining = mobile.Info.WaitAverage + self.World.SharedRandom.Next(-mobile.Info.WaitSpread, mobile.Info.WaitSpread);
					hasWaited = true;
				}

				if (--waitTicksRemaining >= 0)
					return null;

				if (mobile.TicksBeforePathing > 0)
				{
					--mobile.TicksBeforePathing;
					return null;
				}

				// Calculate a new path
				mobile.RemoveInfluence();
				var newPath = EvalPath();
				mobile.AddInfluence();

				if (newPath.Count != 0)
					path = newPath;

				return null;
			}

			hasNotifiedBlocker = false;
			hasWaited = false;
			path.RemoveAt(path.Count - 1);

			var subCell = mobile.GetAvailableSubCell(nextCell, SubCell.Any, ignoreActor);
			return Pair.New(nextCell, subCell);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (path != null)
				return Enumerable.Reverse(path).Select(c => Target.FromCell(self.World, c));
			if (destination != null)
				return new Target[] { Target.FromCell(self.World, destination.Value) };
			return Target.None;
		}

		public override TargetLineNode? TargetLineNode(Actor self)
		{
			return new TargetLineNode(Target.FromCell(self.World, this.destination.Value), Color.Green, false);
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

			public override bool Cancel(Actor self, bool keepQueue = false)
			{
				// MovePart has no child or queued activity so it is correct to cancel this
				// then return the RootActivity's cancel result.
				base.Cancel(self);
				return RootActivity.Cancel(self, keepQueue);
			}

			public override void Queue(Activity activity)
			{
				ParentActivity.Queue(activity);
			}

			public override Activity Tick(Actor self)
			{
				var ret = InnerTick(self, Move.mobile);
				Move.mobile.IsMoving = ret is MovePart;

				if (moveFraction > MoveFractionTotal)
					moveFraction = MoveFractionTotal;
				UpdateCenterLocation(self, Move.mobile);

				return ret;
			}

			Activity InnerTick(Actor self, Mobile mobile)
			{
				moveFraction += mobile.MovementSpeedForCell(self, mobile.ToCell);
				if (moveFraction <= MoveFractionTotal)
					return this;

				var next = OnComplete(self, mobile);
				if (next != null)
					return next;

				return NextActivity;
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

			protected abstract MovePart OnComplete(Actor self, Mobile mobile);

			public override TargetLineNode? TargetLineNode(Actor self)
			{
				//return new TargetLineNode(Target.Invalid, Color.Green, Move);
				return null;
			}
		}

		class MoveFirstHalf : MovePart
		{
			public MoveFirstHalf(Move move, WPos from, WPos to, int fromFacing, int toFacing, int startingFraction)
				: base(move, from, to, fromFacing, toFacing, startingFraction) { }

			static bool IsTurn(Mobile mobile, CPos nextCell)
			{
				return nextCell - mobile.ToCell !=
					mobile.ToCell - mobile.FromCell;
			}

			protected override MovePart OnComplete(Actor self, Mobile mobile)
			{
				var map = self.World.Map;
				var fromSubcellOffset = map.Grid.OffsetOfSubCell(mobile.FromSubCell);
				var toSubcellOffset = map.Grid.OffsetOfSubCell(mobile.ToSubCell);

				if (!IsCanceled || self.Location.Layer == CustomMovementLayerType.Tunnel)
				{
					var nextCell = (ParentActivity as Move).PopPath(self);
					if (nextCell != null)
					{
						if (IsTurn(mobile, nextCell.Value.First))
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

						(ParentActivity as Move).path.Add(nextCell.Value.First);
					}
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

			protected override MovePart OnComplete(Actor self, Mobile mobile)
			{
				mobile.SetPosition(self, mobile.ToCell);
				return null;
			}
		}
	}
}
