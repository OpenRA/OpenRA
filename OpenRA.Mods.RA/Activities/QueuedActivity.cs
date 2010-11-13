#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class QueuedActivity : IActivity
	{
		public QueuedActivity(Action<QueuedActivity> a) { this.a = a; }
		public QueuedActivity(bool runChildOnFirstTick, Action<QueuedActivity> a) : this(a, true, runChildOnFirstTick) { }

		public QueuedActivity(Action<QueuedActivity> a, bool interruptable, bool runChildOnFirstTick)
		{
			this.a = a;
			this.interruptable = interruptable;
			runChildOnFirstTick = runChildOnFirstTick;
		}

		Action<QueuedActivity> a;
		private bool interruptable = true;
		private bool runChild = false;
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self) { return Run(self); }

		public IActivity Run(Actor self)
		{
			if (a != null)
				a(this);
			if (runChild && NextActivity != null)
				return NextActivity.Tick(self);

			return NextActivity;
		}

		public void Insert(IActivity activity)
		{
			if (activity == null)
				return;
			activity.Queue(NextActivity);
			NextActivity = activity;
		}

		public void Cancel(Actor self)
		{
			if (!interruptable)
				return;
			
			a = null;
			NextActivity = null;
		}

		public void Queue( IActivity activity )
		{
			if( NextActivity != null )
				NextActivity.Queue( activity );
			else
				NextActivity = activity;
		}

		public IEnumerable<float2> GetCurrentPath()
		{
			if (NextActivity != null)
				foreach (var path in NextActivity.GetCurrentPath())
				{
					yield return path;
				}

			yield break;
		}
	}
}
