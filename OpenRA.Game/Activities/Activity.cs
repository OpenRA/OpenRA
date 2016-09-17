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
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Activities
{
	public enum ActivityState { Queued, Active, Done, Canceled }

	/*
	 * Activities are actions carried out by actors during each tick.
	 *
	 * Activities exist in a graph data structure built up amongst themselves. Each activity has a parent activity,
	 * optionally child activities, and usually a next activity. An actor's CurrentActivity is a pointer into that graph
	 * and moves through it as activities run.
	 *
	 * There are two kinds of activities, the base activity and composite activities. They differ in the way their children
	 * are run: while a base activity is responsible for running its children itself, a composite activity relies on the actor's
	 * activity-running code. Therefore, the actor's CurrentActivity stays on the base activity while it runs its children. With
	 * composite activities however, the CurrentActivity moves through the list of children as they run.
	 *
	 *
	 * Things to be aware of when writing activities:
	 *
	 * - Use "return NextActivity" at least once somewhere in the tick method.
	 * - Do not use "return new SomeActivity()" as that will break the graph. Queue the new activity and use "return NextActivity" instead.
	 * - Do not "reuse" (with "SequenceActivities", for example) activity objects that have already finished running.
	 *   Queue a new instance instead.
	 * - Avoid calling actor.CancelActivity(). It is almost always a bug. Call activity.Cancel() instead.
	 * - A composite activity will run at least twice. The first time when it returns its children,
	 *   the second time when its last child returns its Parent.
	 * - Do not return the Parent explicitly unless you have an extremly good reason. "return NextActivity"
	 *   will do the right thing in all circumstances.
	 * - You do not need to care about the ChildActivity pointer advancing through the list of children,
	 *   the activity code already takes care of that.
	 * - If you want to check whether there are any follow-up activities queued, check against "NextInQueue"
	 *   in favour of "NextActivity" to avoid checking against the Parent activity.
	 *
	 *
	 * Guide when to use which kind of activity:
	 *
	 * - The activity does not have any children -> base activity
	 * - The activity needs to run preparatory steps during each tick before its children can be run -> base activity
	 * - The activity or the actor is left in a bogus state when one of the child activities is canceled -> base activity
	 * - The activity's children are self-contained and can run independently of the parent -> composite activity
	 * - The activity does not have any or little logic of its own, but is just composed of sub-steps -> composite activity
	*/
	public abstract class Activity
	{
		public ActivityState State { get; private set; }
		public bool IsCanceled { get; private set; }
		/// <summary>
		/// Returns the top-most activity *from the point of view of the calling activity*. Note that the root activity
		/// can and likely will have next activities of its own, which would in turn be the root for their children.
		/// </summary>
		public Activity RootActivity
		{
			get
			{
				var p = this;
				while (p.ParentActivity != null)
					p = p.ParentActivity;

				return p;
			}
		}

		Activity parentActivity;
		public Activity ParentActivity
		{
			get
			{
				return parentActivity;
			}

			protected set
			{
				parentActivity = value;

				var next = NextInQueue;
				if (next != null)
					next.ParentActivity = parentActivity;
			}
		}

		Activity childActivity;
		protected Activity ChildActivity
		{
			get
			{
				return childActivity != null && childActivity.State < ActivityState.Done ? childActivity : null;
			}

			set
			{
				if (value == this || value == ParentActivity || value == NextInQueue)
					childActivity = null;
				else
				{
					childActivity = value;

					if (childActivity != null)
						childActivity.ParentActivity = this;
				}
			}
		}

		Activity nextActivity;

		/// <summary>
		/// The getter will return either the next activity or, if there is none, the parent one.
		/// </summary>
		public virtual Activity NextActivity
		{
			get
			{
				return nextActivity != null ? nextActivity : ParentActivity;
			}

			set
			{
				if (value == this || value == ParentActivity || (value != null && value.ParentActivity == this))
					nextActivity = null;
				else
				{
					nextActivity = value;

					if (nextActivity != null)
						nextActivity.ParentActivity = ParentActivity;
				}
			}
		}

		/// <summary>
		/// The getter will return the next activity on the same level _only_, in contrast to NextActivity.
		/// Use this to check whether there are any follow-up activities queued.
		/// </summary>
		public Activity NextInQueue
		{
			get { return nextActivity; }
			set { NextActivity = value; }
		}

		public bool IsIdle { get; protected set; }
		public bool IsInterruptible { get; protected set; }
		public bool IsCanceled { get { return State == ActivityState.Canceled; } }

		public Activity()
		{
			IsInterruptible = true;
		}

		public Activity TickOuter(Actor self)
		{
			if (State == ActivityState.Done && Game.Settings.Debug.StrictActivityChecking)
				throw new InvalidOperationException("Actor {0} attempted to tick activity {1} after it had already completed.".F(self, this.GetType()));

			if (State == ActivityState.Queued)
			{
				OnFirstRun(self);
				State = ActivityState.Active;
			}

			var ret = Tick(self);
			if (ret == null || (ret != this && ret.ParentActivity != this))
			{
				// Make sure that the Parent's ChildActivity pointer is moved forwards as the child queue advances.
				// The Child's ParentActivity will be set automatically during assignment.
				if (ParentActivity != null && ParentActivity != ret)
					ParentActivity.ChildActivity = ret;

				if (State != ActivityState.Canceled)
					State = ActivityState.Done;

				OnLastRun(self);
			}

			return ret;
		}

		public abstract Activity Tick(Actor self);

		/// <summary>
		/// Runs once immediately before the first Tick() execution.
		/// </summary>
		protected virtual void OnFirstRun(Actor self) { }

		/// <summary>
		/// Runs once immediately after the last Tick() execution.
		/// </summary>
		protected virtual void OnLastRun(Actor self) { }

		public virtual bool Cancel(Actor self, bool keepQueue = false)
		{
			if (!IsInterruptible)
				return false;

			if (ChildActivity != null && !ChildActivity.Cancel(self))
				return false;

			if (!keepQueue)
				NextActivity = null;

			ChildActivity = null;
			State = ActivityState.Canceled;

			return true;
		}

		public virtual void Queue(Activity activity)
		{
			if (NextInQueue != null)
				NextInQueue.Queue(activity);
			else
				NextInQueue = activity;
		}

		public virtual void QueueChild(Activity activity)
		{
			if (ChildActivity != null)
				ChildActivity.Queue(activity);
			else
				ChildActivity = activity;
		}

		/// <summary>
		/// Prints the activity tree, starting from the root or optionally from a given origin.
		///
		/// Call this method from any place that's called during a tick, such as the Tick() method itself or
		/// the Before(First|Last)Run() methods. The origin activity will be marked in the output.
		/// </summary>
		/// <param name="origin">Activity from which to start traversing, and which to mark. If null, mark the calling activity, and start traversal from the root.</param>
		/// <param name="level">Initial level of indentation.</param>
		protected void PrintActivityTree(Activity origin = null, int level = 0)
		{
			if (origin == null)
				RootActivity.PrintActivityTree(this);
			else
			{
				Console.Write(new string(' ', level * 2));
				if (origin == this)
					Console.Write("*");

				Console.WriteLine(this.GetType().ToString().Split('.').Last());

				if (ChildActivity != null)
					ChildActivity.PrintActivityTree(origin, level + 1);

				if (NextInQueue != null)
					NextInQueue.PrintActivityTree(origin, level);
			}
		}

		public virtual KeyValuePair<Target?, Color?> GetTargets(Actor self)
		{
			return new KeyValuePair<Target?, Color?>(null, null);
		}
	}

	/// <summary>
	/// In contrast to the base activity class, which is responsible for running its children itself,
	/// composite activities rely on the actor's activity-running logic for their children.
	/// </summary>
	public abstract class CompositeActivity : Activity
	{
		/// <summary>
		/// The getter will return the first non-null value of either child, next or parent activity, in that order, or ultimately null.
		/// </summary>
		public override Activity NextActivity
		{
			get
			{
				if (ChildActivity != null)
					return ChildActivity;
				else if (NextInQueue != null)
					return NextInQueue;
				else
					return ParentActivity;
			}
		}
	}

	public static class ActivityExts
	{
/*
		public static IEnumerable<Target> GetTargetQueue(this Actor self)
		{
			return self.CurrentActivity
				.Iterate(u => u.NextActivity)
				.TakeWhile(u => u != null)
				.SelectMany(u => u.GetTargets(self));
		}
*/
	}
}
