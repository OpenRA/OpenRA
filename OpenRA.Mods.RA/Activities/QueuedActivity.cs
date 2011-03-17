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
using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class QueuedActivity : Activity
	{
		public QueuedActivity(Action<QueuedActivity> a) { this.a = a; }

		public QueuedActivity(Action<QueuedActivity> a, bool interruptable)
		{
			this.a = a;
		}

		Action<QueuedActivity> a;

		public override Activity Tick(Actor self) { return Run(self); }

		public Activity Run(Actor self)
		{
			if (a != null)
				a(this);

			return NextActivity;
		}

		public void Insert( Activity activity )
		{
			if (activity == null)
				return;
			activity.Queue(NextActivity);
			NextActivity = activity;
		}

		public override IEnumerable<Target> GetTargetQueue( Actor self )
		{
			if (NextActivity != null)
				foreach (var target in NextActivity.GetTargetQueue(self))
				{
					yield return target;
				}

			yield break;
		}
	}
}
