#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;

namespace OpenRA.Primitives
{
	/// <summary>
	/// A thread-safe action queue, suitable for passing units of work between threads.
	/// </summary>
	public class ActionQueue
	{
		object syncRoot = new object();
		PriorityQueue<DelayedAction> actions = new PriorityQueue<DelayedAction>();

		public void Add(Action a) { Add(a, 0); }
		public void Add(Action a, int delay)
		{
			lock (syncRoot)
				actions.Add(new DelayedAction(a, Game.RunTime + delay));
		}

		public void PerformActions()
		{
			Action a = () => {};
			lock (syncRoot)
			{
				var t = Game.RunTime;
				while (!actions.Empty && actions.Peek().Time <= t)
				{
					var da = actions.Pop();
					a = da.Action + a;
				}
			}
			a();
		}
	}

	struct DelayedAction : IComparable<DelayedAction>
	{
		public int Time;
		public Action Action;

		public DelayedAction(Action action, int time)
		{
			Action = action;
			Time = time;
		}

		public int CompareTo(DelayedAction other)
		{
			return Time.CompareTo(other.Time);
		}
	}
}
