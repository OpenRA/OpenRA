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

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public enum EnterBehaviour { Exit, Suicide, Dispose }

	public abstract class Enter : Activity
	{
		public enum ReserveStatus { None, TooFar, Pending, Ready }
		enum State { ApproachingOrEntering, Inside, Exiting, Done }

		readonly IMove move;
		readonly int maxTries = 0;
		readonly EnterBehaviour enterBehaviour;
		readonly bool targetCenter;

		public Target Target { get { return target; } }
		Target target;
		State nextState = State.ApproachingOrEntering; // Hint/starting point for next state
		bool isEnteringOrInside = false; // Used to know if exiting should be used
		WPos savedPos; // Position just before entering
		Activity inner;
		bool firstApproach = true;

		protected Enter(Actor self, Actor target, EnterBehaviour enterBehaviour, int maxTries = 1, bool targetCenter = false)
		{
			move = self.Trait<IMove>();
			this.target = Target.FromActor(target);
			this.maxTries = maxTries;
			this.enterBehaviour = enterBehaviour;
			this.targetCenter = targetCenter;
		}

		// CanEnter(target) should to be true; otherwise, Enter may abort.
		// Tries counter starts at 1 (reset every tick)
		protected virtual bool TryGetAlternateTarget(Actor self, int tries, ref Target target) { return false; }
		protected virtual bool CanReserve(Actor self) { return true; }
		protected virtual ReserveStatus Reserve(Actor self)
		{
			return !CanReserve(self) ? ReserveStatus.None : move.CanEnterTargetNow(self, target) ? ReserveStatus.Ready : ReserveStatus.TooFar;
		}

		protected virtual void Unreserve(Actor self, bool abort) { }
		protected virtual void OnInside(Actor self) { }

		protected bool TryGetAlternateTargetInCircle(
			Actor self, WDist radius, Action<Target> update, Func<Actor, bool> primaryFilter, Func<Actor, bool>[] preferenceFilters = null)
		{
			var diff = new WVec(radius, radius, WDist.Zero);
			var candidates = self.World.ActorMap.ActorsInBox(self.CenterPosition - diff, self.CenterPosition + diff)
				.Where(primaryFilter).Select(a => new { Actor = a, Ls = (self.CenterPosition - a.CenterPosition).HorizontalLengthSquared })
				.Where(p => p.Ls <= radius.LengthSquared).OrderBy(p => p.Ls).Select(p => p.Actor);
			if (preferenceFilters != null)
				foreach (var filter in preferenceFilters)
				{
					var preferredCandidate = candidates.FirstOrDefault(filter);
					if (preferredCandidate == null)
						continue;
					target = Target.FromActor(preferredCandidate);
					update(target);
					return true;
				}

			var candidate = candidates.FirstOrDefault();
			if (candidate == null)
				return false;
			target = Target.FromActor(candidate);
			update(target);
			return true;
		}

		// Called when inner activity is this and returns inner activity for next tick.
		protected virtual Activity InsideTick(Actor self) { return null; }

		// Abort entering and/or leave if necessary
		protected virtual void AbortOrExit(Actor self)
		{
			if (nextState == State.Done)
				return;
			nextState = isEnteringOrInside ? State.Exiting : State.Done;
			if (inner == this)
				inner = null;
			else if (inner != null)
				inner.Cancel(self);
			if (isEnteringOrInside)
				Unreserve(self, true);
		}

		// Cancel inner activity and mark as done unless already leaving or done
		protected void Done(Actor self)
		{
			if (nextState == State.Done)
				return;
			nextState = State.Done;
			if (inner == this)
				inner = null;
			else if (inner != null)
				inner.Cancel(self);
		}

		public override void Cancel(Actor self)
		{
			AbortOrExit(self);
			if (nextState < State.Exiting)
				base.Cancel(self);
			else
				NextActivity = null;
		}

		ReserveStatus TryReserveElseTryAlternateReserve(Actor self)
		{
			for (var tries = 0;;)
				switch (Reserve(self))
				{
					case ReserveStatus.None:
						if (++tries > maxTries || !TryGetAlternateTarget(self, tries, ref target))
							return ReserveStatus.None;
						continue;
					case ReserveStatus.TooFar:
						// Always goto to transport on first approach
						if (firstApproach)
						{
							firstApproach = false;
							return ReserveStatus.TooFar;
						}

						if (++tries > maxTries)
							return ReserveStatus.TooFar;
						Target t = target;
						if (!TryGetAlternateTarget(self, tries, ref t))
							return ReserveStatus.TooFar;
						if ((target.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= (t.CenterPosition - self.CenterPosition).HorizontalLengthSquared)
							return ReserveStatus.TooFar;
						target = t;
						continue;
					case ReserveStatus.Pending:
						return ReserveStatus.Pending;
					case ReserveStatus.Ready:
						return ReserveStatus.Ready;
				}
		}

		State FindAndTransitionToNextState(Actor self)
		{
			switch (nextState)
			{
				case State.ApproachingOrEntering:

					// Reserve to enter or approach
					isEnteringOrInside = false;
					switch (TryReserveElseTryAlternateReserve(self))
					{
						case ReserveStatus.None:
							return State.Done; // No available target -> abort to next activity
						case ReserveStatus.TooFar:
							inner = move.MoveToTarget(self, targetCenter ? Target.FromPos(target.CenterPosition) : target); // Approach
							return State.ApproachingOrEntering;
						case ReserveStatus.Pending:
							return State.ApproachingOrEntering; // Retry next tick
						case ReserveStatus.Ready:
							break; // Reserved target -> start entering target
					}

					// Entering
					isEnteringOrInside = true;
					savedPos = self.CenterPosition; // Save position of self, before entering, for returning on exit

					inner = move.MoveIntoTarget(self, target); // Enter

					if (inner != null)
					{
						nextState = State.Inside; // Should be inside once inner activity is null
						return State.ApproachingOrEntering;
					}

					// Can enter but there is no activity for it, so go inside without one
					goto case State.Inside;

				case State.Inside:
					// Might as well teleport into target if there is no MoveIntoTarget activity
					if (nextState == State.ApproachingOrEntering)
						nextState = State.Inside;

					// Otherwise, try to recover from moving target
					else if (target.CenterPosition != self.CenterPosition)
					{
						nextState = State.ApproachingOrEntering;
						Unreserve(self, false);
						if (Reserve(self) == ReserveStatus.Ready)
						{
							inner = move.MoveIntoTarget(self, target); // Enter
							if (inner != null)
								return State.ApproachingOrEntering;

							nextState = State.ApproachingOrEntering;
							goto case State.ApproachingOrEntering;
						}

						nextState = State.ApproachingOrEntering;
						isEnteringOrInside = false;
						inner = move.MoveIntoWorld(self, self.World.Map.CellContaining(savedPos));

						return State.ApproachingOrEntering;
					}

					OnInside(self);

					if (enterBehaviour == EnterBehaviour.Suicide)
						self.Kill(self);
					else if (enterBehaviour == EnterBehaviour.Dispose)
						self.Dispose();

					// Return if Abort(Actor) or Done(self) was called from OnInside.
					if (nextState >= State.Exiting)
						return State.Inside;

					inner = this; // Start inside activity
					nextState = State.Exiting; // Exit once inner activity is null (unless Done(self) is called)
					return State.Inside;

				// TODO: Handle target moved while inside or always call done for movable targets and use a separate exit activity
				case State.Exiting:
					inner = move.MoveIntoWorld(self, self.World.Map.CellContaining(savedPos));

					// If not successfully exiting, retry on next tick
					if (inner == null)
						return State.Exiting;
					isEnteringOrInside = false;
					nextState = State.Done;
					return State.Exiting;

				case State.Done:
					return State.Done;
			}

			return State.Done; // dummy to quiet dumb compiler
		}

		Activity CanceledTick(Actor self)
		{
			if (inner == null)
				return ActivityUtils.RunActivity(self, NextActivity);
			inner.Cancel(self);
			inner.Queue(NextActivity);
			return ActivityUtils.RunActivity(self, inner);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return CanceledTick(self);

			// Check target validity if not exiting or done
			if (nextState != State.Done && (target.Type != TargetType.Actor || !target.IsValidFor(self)))
				AbortOrExit(self);

			// If no current activity, tick next activity
			if (inner == null && FindAndTransitionToNextState(self) == State.Done)
				return CanceledTick(self);

			// Run inner activity/InsideTick
			inner = inner == this ? InsideTick(self) : ActivityUtils.RunActivity(self, inner);

			// If we are finished, move on to next activity
			if (inner == null && nextState == State.Done)
				return NextActivity;

			return this;
		}
	}
}
