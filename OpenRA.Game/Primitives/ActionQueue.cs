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

using System;
using System.Collections.Generic;

namespace OpenRA.Primitives
{
	/// <summary>
	/// A thread-safe action queue, suitable for passing units of work between threads.
	/// </summary>
	public class ActionQueue
	{
		readonly List<DelayedAction> actions = new();

		public void Add(Action a, long desiredTime)
		{
			if (a == null)
				throw new ArgumentNullException(nameof(a));

			lock (actions)
			{
				var action = new DelayedAction(a, desiredTime);
				var index = Index(action);
				actions.Insert(index, action);
			}
		}

		public void PerformActions(long currentTime)
		{
			DelayedAction[] pendingActions;
			lock (actions)
			{
				var dummyAction = new DelayedAction(null, currentTime);
				var index = Index(dummyAction);
				if (index <= 0)
					return;

				pendingActions = new DelayedAction[index];
				actions.CopyTo(0, pendingActions, 0, index);
				actions.RemoveRange(0, index);
			}

			foreach (var delayedAction in pendingActions)
				delayedAction.Action();
		}

		int Index(DelayedAction action)
		{
			// Returns the index of the next action with a strictly greater time.
			var index = actions.BinarySearch(action, DelayedAction.TimeComparer);
			if (index < 0)
				return ~index;
			while (index < actions.Count && DelayedAction.TimeComparer.Compare(action, actions[index]) >= 0)
				index++;
			return index;
		}
	}

	readonly struct DelayedAction
	{
		sealed class DelayedActionTimeComparer : IComparer<DelayedAction>
		{
			public int Compare(DelayedAction x, DelayedAction y)
			{
				return x.Time.CompareTo(y.Time);
			}
		}

		public static IComparer<DelayedAction> TimeComparer = new DelayedActionTimeComparer();

		public readonly long Time;
		public readonly Action Action;

		public DelayedAction(Action action, long time)
		{
			Action = action;
			Time = time;
		}

		public override string ToString()
		{
			return "Time: " + Time + " Action: " + Action;
		}
	}
}
