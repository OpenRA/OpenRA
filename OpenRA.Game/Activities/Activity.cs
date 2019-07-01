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
using OpenRA.Traits;

namespace OpenRA.Activities
{
	public enum ActivityState { Queued, Active, Canceling, Done }

	/*
	 * Things to be aware of when writing activities:
	 *
	 * - Use "return true" at least once somewhere in the tick method.
	 * - Do not "reuse" (with "SequenceActivities", for example) activity objects that have already started running.
	 *   Queue a new instance instead.
	 * - Avoid calling actor.CancelActivity(). It is almost always a bug. Call activity.Cancel() instead.
	 */
	public abstract class Activity : IActivityInterface
	{
		public ActivityState State { get; private set; }

		protected Activity ChildActivity { get; private set; }
		public Activity NextActivity { get; private set; }

		public bool IsInterruptible { get; protected set; }
		public bool ChildHasPriority { get; protected set; }
		public bool IsCanceling { get { return State == ActivityState.Canceling; } }
		bool finishing;
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
				State = ActivityState.Active;
			}

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
			if (ChildActivity != null && ChildActivity.State == ActivityState.Queued)
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
			if (ChildActivity != null)
				ChildActivity.OnActorDisposeOuter(self);

			OnActorDispose(self);
		}

		public virtual void Cancel(Actor self, bool keepQueue = false)
		{
			if (!keepQueue)
				NextActivity = null;

			if (!IsInterruptible)
				return;

			if (ChildActivity != null)
				ChildActivity.Cancel(self);

			State = ActivityState.Canceling;
		}

		public void Queue(Activity activity)
		{
			if (NextActivity != null)
				NextActivity.Queue(activity);
			else
				NextActivity = activity;
		}

		public void QueueChild(Activity activity)
		{
			if (ChildActivity != null)
				ChildActivity.Queue(activity);
			else
				ChildActivity = activity;
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

				if (ChildActivity != null)
					ChildActivity.PrintActivityTree(self, origin, level + 1);

				if (NextActivity != null)
					NextActivity.PrintActivityTree(self, origin, level);
			}
		}

		public virtual IEnumerable<Target> GetTargets(Actor self)
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
			if (includeChildren && ChildActivity != null)
				foreach (var a in ChildActivity.ActivitiesImplementing<T>())
					yield return a;

			if (this is T)
				yield return (T)(object)this;

			if (NextActivity != null)
				foreach (var a in NextActivity.ActivitiesImplementing<T>())
					yield return a;
		}
	}
}
