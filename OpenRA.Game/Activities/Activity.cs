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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Activities
{
	public enum ActivityState { Queued, Active, Canceling, Done }

	public class TargetLineNode
	{
		public readonly Target Target;
		public readonly Color Color;
		public readonly Sprite Tile;

		public TargetLineNode(in Target target, Color color, Sprite tile = null)
		{
			// Note: Not all activities are drawable. In that case, pass Target.Invalid as target,
			// if "yield break" in TargetLineNode(Actor self) is not feasible.
			Target = target;
			Color = color;
			Tile = tile;
		}
	}

	/*
	 * Things to be aware of when writing activities:
	 *
	 * - Use "return true" at least once somewhere in the tick method.
	 * - Do not "reuse" activity objects (by queuing them as next or child, for example) that have already started running.
	 *   Queue a new instance instead.
	 * - Avoid calling actor.CancelActivity(). It is almost always a bug. Call activity.Cancel() instead.
	 * - Do not evaluate dynamic state (an actor's location, health, conditions, etc.) in the activity's constructor,
	 *   as that might change before the activity gets to tick for the first time.  Use the OnFirstRun() method instead.
	 */
	public abstract class Activity : IActivityInterface
	{
		public ActivityState State { get; private set; }

		Activity childActivity;
		protected Activity ChildActivity
		{
			get { return SkipDoneActivities(childActivity); }
			private set { childActivity = value; }
		}

		Activity nextActivity;
		public Activity NextActivity
		{
			get { return SkipDoneActivities(nextActivity); }
			private set { nextActivity = value; }
		}

		internal static Activity SkipDoneActivities(Activity first)
		{
			// If first.Cancel() was called while it was queued (i.e. before it first ticked), its state will be Done
			// rather than Queued (the activity system guarantees that it cannot be Active or Canceling).
			// An unknown number of ticks may have elapsed between the Cancel() call and now,
			// so we cannot make any assumptions on the value of first.NextActivity.
			// We must not return first (ticking it would be bogus), but returning null would potentially
			// drop valid activities queued after it. Walk the queue until we find a valid activity or
			// (more likely) run out of activities.
			while (first != null && first.State == ActivityState.Done)
				first = first.nextActivity;

			return first;
		}

		public bool IsInterruptible { get; protected set; }
		public bool ChildHasPriority { get; protected set; }
		public bool IsCanceling { get { return State == ActivityState.Canceling; } }
		bool finishing;
		bool firstRunCompleted;
		bool lastRun;

		public Activity()
		{
			IsInterruptible = true;
			ChildHasPriority = true;
		}

		public Activity TickOuter(Actor self)
		{
			if (State == ActivityState.Done)
				throw new InvalidOperationException("Actor {0} attempted to tick activity {1} after it had already completed.".F(self, GetType()));

			if (State == ActivityState.Queued)
			{
				OnFirstRun(self);
				firstRunCompleted = true;
				State = ActivityState.Active;
			}

			if (!firstRunCompleted)
				throw new InvalidOperationException("Actor {0} attempted to tick activity {1} before running its OnFirstRun method.".F(self, GetType()));

			// Only run the parent tick when the child is done.
			// We must always let the child finish on its own before continuing.
			if (ChildHasPriority)
			{
				lastRun = TickChild(self) && (finishing || Tick(self));
				finishing |= lastRun;
			}

			// The parent determines whether the child gets a chance at ticking.
			else
				lastRun = Tick(self);

			// Avoid a single tick delay if the childactivity was just queued.
			var ca = ChildActivity;
			if (ca != null && ca.State == ActivityState.Queued)
			{
				if (ChildHasPriority)
					lastRun = TickChild(self) && finishing;
				else
					TickChild(self);
			}

			if (lastRun)
			{
				State = ActivityState.Done;
				OnLastRun(self);
				return NextActivity;
			}

			return this;
		}

		protected bool TickChild(Actor self)
		{
			ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
			return ChildActivity == null;
		}

		/// <summary>
		/// Called every tick to run activity logic. Returns false if the activity should
		/// remain active, or true if it is complete. Cancelled activities must ensure they
		/// return the actor to a consistent state before returning true.
		///
		/// Child activities can be queued using QueueChild, and these will be ticked
		/// instead of the parent while they are active. Activities that need to run logic
		/// in parallel with child activities should set ChildHasPriority to false and
		/// manually call TickChildren.
		///
		/// Queuing one or more child activities and returning true is valid, and causes
		/// the activity to be completed immediately (without ticking again) once the
		/// children have completed.
		/// </summary>
		public virtual bool Tick(Actor self)
		{
			return true;
		}

		/// <summary>
		/// Runs once immediately before the first Tick() execution.
		/// </summary>
		protected virtual void OnFirstRun(Actor self) { }

		/// <summary>
		/// Runs once immediately after the last Tick() execution.
		/// </summary>
		protected virtual void OnLastRun(Actor self) { }

		/// <summary>
		/// Runs once on Actor.Dispose() (through OnActorDisposeOuter) and can be used to perform activity clean-up on actor death/disposal,
		/// for example by force-triggering OnLastRun (which would otherwise be skipped).
		/// </summary>
		protected virtual void OnActorDispose(Actor self) { }

		/// <summary>
		/// Runs once on Actor.Dispose().
		/// Main purpose is to ensure ChildActivity.OnActorDispose runs as well (which isn't otherwise accessible due to protection level).
		/// </summary>
		internal void OnActorDisposeOuter(Actor self)
		{
			ChildActivity?.OnActorDisposeOuter(self);

			OnActorDispose(self);
		}

		public virtual void Cancel(Actor self, bool keepQueue = false)
		{
			if (!keepQueue)
				NextActivity = null;

			if (!IsInterruptible)
				return;

			ChildActivity?.Cancel(self);

			// Directly mark activities that are queued and therefore didn't run yet as done
			State = State == ActivityState.Queued ? ActivityState.Done : ActivityState.Canceling;
		}

		public void Queue(Activity activity)
		{
			var it = this;
			while (it.nextActivity != null)
				it = it.nextActivity;
			it.nextActivity = activity;
		}

		public void QueueChild(Activity activity)
		{
			if (childActivity != null)
				childActivity.Queue(activity);
			else
				childActivity = activity;
		}

		/// <summary>
		/// Prints the activity tree, starting from the top or optionally from a given origin.
		///
		/// Call this method from any place that's called during a tick, such as the Tick() method itself or
		/// the Before(First|Last)Run() methods. The origin activity will be marked in the output.
		/// </summary>
		/// <param name="self">The actor performing this activity.</param>
		/// <param name="origin">Activity from which to start traversing, and which to mark. If null, mark the calling activity, and start traversal from the top.</param>
		/// <param name="level">Initial level of indentation.</param>
		protected void PrintActivityTree(Actor self, Activity origin = null, int level = 0)
		{
			if (origin == null)
				self.CurrentActivity.PrintActivityTree(self, this);
			else
			{
				Console.Write(new string(' ', level * 2));
				if (origin == this)
					Console.Write("*");

				Console.WriteLine(GetType().ToString().Split('.').Last());

				ChildActivity?.PrintActivityTree(self, origin, level + 1);

				NextActivity?.PrintActivityTree(self, origin, level);
			}
		}

		public virtual IEnumerable<Target> GetTargets(Actor self)
		{
			yield break;
		}

		public virtual IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield break;
		}

		public IEnumerable<string> DebugLabelComponents()
		{
			var act = this;
			while (act != null)
			{
				yield return act.GetType().Name;
				act = act.ChildActivity;
			}
		}

		public IEnumerable<T> ActivitiesImplementing<T>(bool includeChildren = true) where T : IActivityInterface
		{
			// Skips Done child and next activities
			if (includeChildren)
			{
				var ca = ChildActivity;
				if (ca != null)
					foreach (var a in ca.ActivitiesImplementing<T>())
						yield return a;
			}

			if (this is T)
				yield return (T)(object)this;

			var na = NextActivity;
			if (na != null)
				foreach (var a in na.ActivitiesImplementing<T>())
					yield return a;
		}
	}
}
