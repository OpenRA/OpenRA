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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Activities
{
	public abstract class Activity
	{
		public Activity NextActivity { get; set; }
		public bool IsCanceled { get; private set; }

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

		public virtual IEnumerable<KeyValuePair<Target, Color>> GetTargets(Actor self)
		{
			return new KeyValuePair<Target, Color>[0];
		}
	}

	public static class ActivityExts
	{
/*
		public static IEnumerable<Target> GetTargetQueue(this Actor self)
		{
			return self.GetCurrentActivity()
				.Iterate(u => u.NextActivity)
				.TakeWhile(u => u != null)
				.SelectMany(u => u.GetTargets(self));
		}
*/
	}
}
