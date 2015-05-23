#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		readonly IEnumerable<IDisableMove> moveDisablers;
		readonly WRange nearEnough;
		readonly Func<List<CPos>> getPath;
		readonly Actor ignoredActor;

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
			moveDisablers = self.TraitsImplementing<IDisableMove>();

			getPath = () =>
				self.World.WorldActor.Trait<IPathFinder>().FindPath(
					PathSearch.FromPoint(self.World, mobile.Info, self, mobile.ToCell, destination, false)
					.WithoutLaneBias());
			this.destination = destination;
			this.nearEnough = WRange.Zero;
		}

		// HACK: for legacy code
		public Move(Actor self, CPos destination, int nearEnough)
			: this(self, destination, WRange.FromCells(nearEnough)) { }

		public Move(Actor self, CPos destination, WRange nearEnough)
		{
			mobile = self.Trait<Mobile>();
			moveDisablers = self.TraitsImplementing<IDisableMove>();

			getPath = () => self.World.WorldActor.Trait<IPathFinder>()
				.FindUnitPath(mobile.ToCell, destination, self);
			this.destination = destination;
			this.nearEnough = nearEnough;
		}

		public Move(Actor self, CPos destination, SubCell subCell, WRange nearEnough)
		{
			mobile = self.Trait<Mobile>();
			moveDisablers = self.TraitsImplementing<IDisableMove>();

			getPath = () => self.World.WorldActor.Trait<IPathFinder>()
				.FindUnitPathToRange(mobile.FromCell, subCell, self.World.Map.CenterOfSubCell(destination, subCell), nearEnough, self);
			this.destination = destination;
			this.nearEnough = nearEnough;
		}

		public Move(Actor self, CPos destination, Actor ignoredActor)
		{
			mobile = self.Trait<Mobile>();
			moveDisablers = self.TraitsImplementing<IDisableMove>();

			getPath = () =>
				self.World.WorldActor.Trait<IPathFinder>().FindPath(
					PathSearch.FromPoint(self.World, mobile.Info, self, mobile.ToCell, destination, false)
					.WithIgnoredActor(ignoredActor));

			this.destination = destination;
			this.nearEnough = WRange.Zero;
			this.ignoredActor = ignoredActor;
		}

		public Move(Actor self, Target target, WRange range)
		{
			mobile = self.Trait<Mobile>();
			moveDisablers = self.TraitsImplementing<IDisableMove>();

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
			moveDisablers = self.TraitsImplementing<IDisableMove>();

			this.getPath = getPath;

			destination = null;
			nearEnough = WRange.Zero;
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
			if (moveDisablers.Any(d => d.MoveDisabled(self)))
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
				return Util.SequenceActivities(new Turn(self, firstFacing), this);
			}
			else
			{
				mobile.SetLocation(mobile.FromCell, mobile.FromSubCell, nextCell.Value.First, nextCell.Value.Second);
				var from = self.World.Map.CenterOfSubCell(mobile.FromCell, mobile.FromSubCell);
				var to = Util.BetweenCells(self.World, mobile.FromCell, mobile.ToCell) +
					(self.World.Map.OffsetOfSubCell(mobile.FromSubCell) +
					self.World.Map.OffsetOfSubCell(mobile.ToSubCell)) / 2;
				var move = new MoveFirstHalf(
					this,
					from,
					to,
					mobile.Facing,
					mobile.Facing,
					0);

				return move;
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

			// Next cell in the move is blocked by another actor
			if (!mobile.CanMoveFreelyInto(nextCell, ignoredActor, true))
			{
				// Are we close enough?
				var cellRange = nearEnough.Range / 1024;
				if ((mobile.ToCell - destination.Value).LengthSquared <= cellRange * cellRange)
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
					var info = self.Info.Traits.Get<MobileInfo>();
					waitTicksRemaining = info.WaitAverage + self.World.SharedRandom.Next(-info.WaitSpread, info.WaitSpread);
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

			var subCell = mobile.GetAvailableSubCell(nextCell, SubCell.Any, ignoredActor);
			return Pair.New(nextCell, subCell);
		}

		public override void Cancel(Actor self)
		{
			path = NoPath;
			base.Cancel(self);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (path != null)
				return Enumerable.Reverse(path).Select(c => Target.FromCell(self.World, c));
			if (destination != null)
				return new Target[] { Target.FromCell(self.World, destination.Value) };
			return Target.None;
		}

		abstract class MovePart : Activity
		{
			protected readonly Move Move;
			protected readonly WPos From, To;
			protected readonly int FromFacing, ToFacing;
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
			}

			public override void Cancel(Actor self)
			{
				Move.Cancel(self);
				base.Cancel(self);
			}

			public override void Queue(Activity activity)
			{
				Move.Queue(activity);
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

				var next = OnComplete(self, mobile, Move);
				if (next != null)
					return next;

				return Move;
			}

			void UpdateCenterLocation(Actor self, Mobile mobile)
			{
				// avoid division through zero
				if (MoveFractionTotal != 0)
					mobile.SetVisualPosition(self, WPos.Lerp(From, To, moveFraction, MoveFractionTotal));
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

			static bool IsTurn(Mobile mobile, CPos nextCell)
			{
				return nextCell - mobile.ToCell !=
					mobile.ToCell - mobile.FromCell;
			}

			protected override MovePart OnComplete(Actor self, Mobile mobile, Move parent)
			{
				var fromSubcellOffset = self.World.Map.OffsetOfSubCell(mobile.FromSubCell);
				var toSubcellOffset = self.World.Map.OffsetOfSubCell(mobile.ToSubCell);

				var nextCell = parent.PopPath(self);
				if (nextCell != null)
				{
					if (IsTurn(mobile, nextCell.Value.First))
					{
						var nextSubcellOffset = self.World.Map.OffsetOfSubCell(nextCell.Value.Second);
						var ret = new MoveFirstHalf(
							Move,
							Util.BetweenCells(self.World, mobile.FromCell, mobile.ToCell) + (fromSubcellOffset + toSubcellOffset) / 2,
							Util.BetweenCells(self.World, mobile.ToCell, nextCell.Value.First) + (toSubcellOffset + nextSubcellOffset) / 2,
							mobile.Facing,
							Util.GetNearestFacing(mobile.Facing, self.World.Map.FacingBetween(mobile.ToCell, nextCell.Value.First, mobile.Facing)),
							moveFraction - MoveFractionTotal);

						mobile.FinishedMoving(self);
						mobile.SetLocation(mobile.ToCell, mobile.ToSubCell, nextCell.Value.First, nextCell.Value.Second);
						return ret;
					}

					parent.path.Add(nextCell.Value.First);
				}

				var ret2 = new MoveSecondHalf(
					Move,
					Util.BetweenCells(self.World, mobile.FromCell, mobile.ToCell) + (fromSubcellOffset + toSubcellOffset) / 2,
					self.World.Map.CenterOfCell(mobile.ToCell) + toSubcellOffset,
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

	public static class ActorExtensionsForMove
	{
		public static bool IsMoving(this Actor self)
		{
			var a = self.GetCurrentActivity();
			if (a == null)
				return false;

			// HACK: Dirty, but it suffices until we do something better:
			if (a.GetType() == typeof(Move)) return true;
			if (a.GetType() == typeof(MoveAdjacentTo)) return true;
			if (a.GetType() == typeof(AttackMoveActivity)) return true;

			// Not a move:
			return false;
		}
	}
}
