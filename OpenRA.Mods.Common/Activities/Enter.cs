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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public enum EnterBehaviour { Exit, Suicide, Dispose }

	public abstract class Enter : Activity
	{
		enum EnterState { Approaching, Entering, Exiting }

		readonly IMove move;
		readonly Color? targetLineColor;

		Target target;
		Target lastVisibleTarget;
		bool useLastVisibleTarget;
		EnterState lastState = EnterState.Approaching;

		protected Enter(Actor self, Target target, Color? targetLineColor = null)
		{
			move = self.Trait<IMove>();
			this.target = target;
			this.targetLineColor = targetLineColor;
		}

		/// <summary>
		/// Called early in the activity tick to allow subclasses to update state.
		/// Call Cancel(self, true) if it is no longer valid to enter
		/// </summary>
		protected virtual void TickInner(Actor self, Target target, bool targetIsDeadOrHiddenActor) { }

		/// <summary>
		/// Called when the actor is ready to transition from approaching to entering the target actor.
		/// Return true to start entering, or false to wait in the WaitingToEnter state.
		/// Call Cancel(self, true) before returning false if it is no longer valid to enter
		/// </summary>
		protected virtual bool TryStartEnter(Actor self, Actor targetActor) { return true; }

		/// <summary>
		/// Called when the actor has entered the target actor
		/// Return true if the action succeeded and the actor should be Killed/Disposed
		/// (assuming the relevant EnterBehaviour), or false if the actor should exit unharmed
		/// </summary>
		protected virtual void OnEnterComplete(Actor self, Actor targetActor) { }

		public override Activity Tick(Actor self)
		{
			// Update our view of the target
			bool targetIsHiddenActor;
			target = target.Recalculate(self.Owner, out targetIsHiddenActor);
			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
				lastVisibleTarget = Target.FromTargetPositions(target);

			var oldUseLastVisibleTarget = useLastVisibleTarget;
			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// Cancel immediately if the target died while we were entering it
			if (!IsCanceling && useLastVisibleTarget && lastState == EnterState.Entering)
				Cancel(self, true);

			TickInner(self, target, useLastVisibleTarget);

			// Update target lines if required
			if (useLastVisibleTarget != oldUseLastVisibleTarget && targetLineColor.HasValue)
				self.SetTargetLine(useLastVisibleTarget ? lastVisibleTarget : target, targetLineColor.Value, false);

			// We need to wait for movement to finish before transitioning to
			// the next state or next activity
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			// Note that lastState refers to what we have just *finished* doing
			switch (lastState)
			{
				case EnterState.Approaching:
				{
					// NOTE: We can safely cancel in this case because we know the
					// actor has finished any in-progress move activities
					if (IsCanceling)
						return NextActivity;

					// Lost track of the target
					if (useLastVisibleTarget && lastVisibleTarget.Type == TargetType.Invalid)
						return NextActivity;

					// We are not next to the target - lets fix that
					if (target.Type != TargetType.Invalid && !move.CanEnterTargetNow(self, target))
					{
						// Target lines are managed by this trait, so we do not pass targetLineColor
						var initialTargetPosition = (useLastVisibleTarget ? lastVisibleTarget : target).CenterPosition;
						QueueChild(self, move.MoveToTarget(self, target, initialTargetPosition), true);
						return this;
					}

					// We are next to where we thought the target should be, but it isn't here
					// There's not much more we can do here
					if (useLastVisibleTarget || target.Type != TargetType.Actor)
						return NextActivity;

					// Are we ready to move into the target?
					if (TryStartEnter(self, target.Actor))
					{
						lastState = EnterState.Entering;
						QueueChild(self, move.MoveIntoTarget(self, target), true);
						return this;
					}

					// Subclasses can cancel the activity during TryStartEnter
					// Return immediately to avoid an extra tick's delay
					if (IsCanceling)
						return NextActivity;

					return this;
				}

				case EnterState.Entering:
				{
					// Check that we reached the requested position
					var targetPos = target.Positions.PositionClosestTo(self.CenterPosition);
					if (!IsCanceling && self.CenterPosition == targetPos && target.Type == TargetType.Actor)
						OnEnterComplete(self, target.Actor);

					lastState = EnterState.Exiting;
					QueueChild(self, move.MoveIntoWorld(self, self.Location), true);
					return this;
				}

				case EnterState.Exiting:
					return NextActivity;
			}

			return this;
		}
	}
}
