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
using System.Diagnostics;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Move
{
	class Move : Activity
	{
		static readonly List<CPos> NoPath = new List<CPos>();

		CPos? destination;
		WRange nearEnough;
		List<CPos> path;
		Func<Actor, Mobile, List<CPos>> getPath;
		Actor ignoreBuilding;

		// For dealing with blockers
		bool hasWaited;
		bool hasNotifiedBlocker;
		int waitTicksRemaining;

		// Scriptable move order
		// Ignores lane bias and nearby units
		public Move(CPos destination)
		{
			this.getPath = (self, mobile) =>
				self.World.WorldActor.Trait<PathFinder>().FindPath(
					PathSearch.FromPoint(self.World, mobile.Info, self, mobile.toCell, destination, false)
					.WithoutLaneBias());
			this.destination = destination;
			this.nearEnough = WRange.Zero;
		}

		// HACK: for legacy code
		public Move(CPos destination, int nearEnough)
			: this(destination, WRange.FromCells(nearEnough)) { }

		public Move(CPos destination, WRange nearEnough)
		{
			this.getPath = (self, mobile) => self.World.WorldActor.Trait<PathFinder>()
				.FindUnitPath(mobile.toCell, destination, self);
			this.destination = destination;
			this.nearEnough = nearEnough;
		}

		public Move(CPos destination, SubCell subCell, WRange nearEnough)
		{
			this.getPath = (self, mobile) => self.World.WorldActor.Trait<PathFinder>()
				.FindUnitPathToRange(mobile.fromCell, subCell, self.World.Map.CenterOfSubCell(destination, subCell), nearEnough, self);
			this.destination = destination;
			this.nearEnough = nearEnough;
		}

		public Move(CPos destination, Actor ignoreBuilding)
		{
			this.getPath = (self, mobile) =>
				self.World.WorldActor.Trait<PathFinder>().FindPath(
					PathSearch.FromPoint(self.World, mobile.Info, self, mobile.toCell, destination, false)
					.WithIgnoredBuilding(ignoreBuilding));

			this.destination = destination;
			this.nearEnough = WRange.Zero;
			this.ignoreBuilding = ignoreBuilding;
		}

		public Move(Target target, WRange range)
		{
			this.getPath = (self, mobile) =>
			{
				if (!target.IsValidFor(self))
					return NoPath;

				return self.World.WorldActor.Trait<PathFinder>().FindUnitPathToRange(
					mobile.toCell, mobile.toSubCell, target.CenterPosition, range, self);
			};

			this.destination = null;
			this.nearEnough = range;
		}

		public Move(Func<List<CPos>> getPath)
		{
			this.getPath = (_1, _2) => getPath();
			this.destination = null;
			this.nearEnough = WRange.Zero;
		}

		static int HashList<T>(List<T> xs)
		{
			var hash = 0;
			var n = 0;
			foreach (var x in xs)
				hash += n++ * x.GetHashCode();

			return hash;
		}

		List<CPos> EvalPath(Actor self, Mobile mobile)
		{
			var path = getPath(self, mobile).TakeWhile(a => a != mobile.toCell).ToList();
			mobile.PathHash = HashList(path);
			return path;
		}

		public override Activity Tick(Actor self)
		{
			var mobile = self.Trait<Mobile>();

			if (destination == mobile.toCell)
				return NextActivity;

			if (path == null)
			{
				if (mobile.ticksBeforePathing > 0)
				{
					--mobile.ticksBeforePathing;
					return this;
				}

				path = EvalPath(self, mobile);
				SanityCheckPath(mobile);
			}

			if (path.Count == 0)
			{
				destination = mobile.toCell;
				return this;
			}

			destination = path[0];

			var nextCell = PopPath(self, mobile);
			if (nextCell == null)
				return this;

			var firstFacing = self.World.Map.FacingBetween(mobile.fromCell, nextCell.Value.First, mobile.Facing);
			if (firstFacing != mobile.Facing)
			{
				path.Add(nextCell.Value.First);
				return Util.SequenceActivities(new Turn(firstFacing), this);
			}
			else
			{
				mobile.SetLocation(mobile.fromCell, mobile.fromSubCell, nextCell.Value.First, nextCell.Value.Second);
				var move = new MoveFirstHalf(
					this,
					self.World.Map.CenterOfSubCell(mobile.fromCell, mobile.fromSubCell),
					Util.BetweenCells(self.World, mobile.fromCell, mobile.toCell) + (self.World.Map.OffsetOfSubCell(mobile.fromSubCell) + self.World.Map.OffsetOfSubCell(mobile.toSubCell)) / 2,
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
			var d = path[path.Count - 1] - mobile.toCell;
			if (d.LengthSquared > 2)
				throw new InvalidOperationException("(Move) Sanity check failed");
		}

		static void NotifyBlocker(Actor self, CPos nextCell)
		{
			foreach (var blocker in self.World.ActorMap.GetUnitsAt(nextCell))
			{
				// Notify the blocker that he's blocking our move:
				foreach (var moveBlocked in blocker.TraitsImplementing<INotifyBlockingMove>())
					moveBlocked.OnNotifyBlockingMove(blocker, self);
			}
		}

		Pair<CPos, SubCell>? PopPath(Actor self, Mobile mobile)
		{
			if (path.Count == 0)
				return null;

			var nextCell = path[path.Count - 1];

			// Next cell in the move is blocked by another actor
			if (!mobile.CanEnterCell(nextCell, ignoreBuilding, true))
			{
				// Are we close enough?
				var cellRange = nearEnough.Range / 1024;
				if ((mobile.toCell - destination.Value).LengthSquared <= cellRange * cellRange)
				{
					path.Clear();
					return null;
				}

				// See if they will move
				if (!hasNotifiedBlocker)
				{
					NotifyBlocker(self, nextCell);
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

				if (mobile.ticksBeforePathing > 0)
				{
					--mobile.ticksBeforePathing;
					return null;
				}

				// Calculate a new path
				mobile.RemoveInfluence();
				var newPath = EvalPath(self, mobile);
				mobile.AddInfluence();

				if (newPath.Count != 0)
					path = newPath;

				return null;
			}

			hasNotifiedBlocker = false;
			hasWaited = false;
			path.RemoveAt(path.Count - 1);

			var subCell = mobile.GetAvailableSubCell(nextCell, SubCell.Any, ignoreBuilding);
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
			protected readonly Move move;
			protected readonly WPos from, to;
			protected readonly int fromFacing, toFacing;
			protected readonly int moveFractionTotal;
			protected int moveFraction;

			public MovePart(Move move, WPos from, WPos to, int fromFacing, int toFacing, int startingFraction)
			{
				this.move = move;
				this.from = from;
				this.to = to;
				this.fromFacing = fromFacing;
				this.toFacing = toFacing;
				this.moveFraction = startingFraction;
				this.moveFractionTotal = (to - from).Length;
			}

			public override void Cancel(Actor self)
			{
				move.Cancel(self);
				base.Cancel(self);
			}

			public override void Queue(Activity activity)
			{
				move.Queue(activity);
			}

			public override Activity Tick(Actor self)
			{
				var mobile = self.Trait<Mobile>();
				var ret = InnerTick(self, mobile);
				mobile.IsMoving = ret is MovePart;

				if (moveFraction > moveFractionTotal)
					moveFraction = moveFractionTotal;
				UpdateCenterLocation(self, mobile);

				return ret;
			}

			Activity InnerTick(Actor self, Mobile mobile)
			{
				moveFraction += mobile.MovementSpeedForCell(self, mobile.toCell);
				if (moveFraction <= moveFractionTotal)
					return this;

				var next = OnComplete(self, mobile, move);
				if (next != null)
					return next;

				return move;
			}

			void UpdateCenterLocation(Actor self, Mobile mobile)
			{
				// avoid division through zero
				if (moveFractionTotal != 0)
					mobile.SetVisualPosition(self, WPos.Lerp(from, to, moveFraction, moveFractionTotal));
				else
					mobile.SetVisualPosition(self, to);

				if (moveFraction >= moveFractionTotal)
					mobile.Facing = toFacing & 0xFF;
				else
					mobile.Facing = int2.Lerp(fromFacing, toFacing, moveFraction, moveFractionTotal) & 0xFF;
			}

			protected abstract MovePart OnComplete(Actor self, Mobile mobile, Move parent);

			public override IEnumerable<Target> GetTargets(Actor self)
			{
				return move.GetTargets(self);
			}
		}

		class MoveFirstHalf : MovePart
		{
			public MoveFirstHalf(Move move, WPos from, WPos to, int fromFacing, int toFacing, int startingFraction)
				: base(move, from, to, fromFacing, toFacing, startingFraction) { }

			static bool IsTurn(Mobile mobile, CPos nextCell)
			{
				return nextCell - mobile.toCell !=
					mobile.toCell - mobile.fromCell;
			}

			protected override MovePart OnComplete(Actor self, Mobile mobile, Move parent)
			{
				var fromSubcellOffset = self.World.Map.OffsetOfSubCell(mobile.fromSubCell);
				var toSubcellOffset = self.World.Map.OffsetOfSubCell(mobile.toSubCell);

				var nextCell = parent.PopPath(self, mobile);
				if (nextCell != null)
				{
					if (IsTurn(mobile, nextCell.Value.First))
					{
						var nextSubcellOffset = self.World.Map.OffsetOfSubCell(nextCell.Value.Second);
						var ret = new MoveFirstHalf(
							move,
							Util.BetweenCells(self.World, mobile.fromCell, mobile.toCell) + (fromSubcellOffset + toSubcellOffset) / 2,
							Util.BetweenCells(self.World, mobile.toCell, nextCell.Value.First) + (toSubcellOffset + nextSubcellOffset) / 2,
							mobile.Facing,
							Util.GetNearestFacing(mobile.Facing, self.World.Map.FacingBetween(mobile.toCell, nextCell.Value.First, mobile.Facing)),
							moveFraction - moveFractionTotal);

						mobile.SetLocation(mobile.toCell, mobile.toSubCell, nextCell.Value.First, nextCell.Value.Second);
						return ret;
					}

					parent.path.Add(nextCell.Value.First);
				}

				var ret2 = new MoveSecondHalf(
					move,
					Util.BetweenCells(self.World, mobile.fromCell, mobile.toCell) + (fromSubcellOffset + toSubcellOffset) / 2,
					self.World.Map.CenterOfCell(mobile.toCell) + toSubcellOffset,
					mobile.Facing,
					mobile.Facing,
					moveFraction - moveFractionTotal);

				mobile.EnteringCell(self);
				mobile.SetLocation(mobile.toCell, mobile.toSubCell, mobile.toCell, mobile.toSubCell);
				return ret2;
			}
		}

		class MoveSecondHalf : MovePart
		{
			public MoveSecondHalf(Move move, WPos from, WPos to, int fromFacing, int toFacing, int startingFraction)
				: base(move, from, to, fromFacing, toFacing, startingFraction) { }

			protected override MovePart OnComplete(Actor self, Mobile mobile, Move parent)
			{
				mobile.SetPosition(self, mobile.toCell);
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
			if (a.GetType() == typeof(AttackMove)) return true;

			// Not a move:
			return false;
		}
	}
}
