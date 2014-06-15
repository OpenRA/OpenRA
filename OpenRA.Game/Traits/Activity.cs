#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	public abstract class Activity
	{
		public Activity NextActivity { get; set; }
		protected bool IsCanceled { get; private set; }

		public abstract Activity Tick(Actor self);

		public virtual void Cancel(Actor self)
		{
			IsCanceled = true;
			NextActivity = null;
		}

		public virtual void Queue(Activity activity)
		{
			if (NextActivity != null)
				NextActivity.Queue(activity);
			else
				NextActivity = activity;
		}

		public virtual IEnumerable<Target> GetTargets(Actor self)
		{
			yield break;
		}
	}

	public static class ActivityExts
	{
		public static IEnumerable<Target> GetTargetQueue(this Actor self)
		{
			return self.GetCurrentActivity()
				.Iterate(u => u.NextActivity)
				.TakeWhile(u => u != null)
				.SelectMany(u => u.GetTargets(self));
		}
	}
}
