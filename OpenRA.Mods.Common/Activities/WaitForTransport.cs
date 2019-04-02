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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class WaitForTransport : Activity
	{
		readonly ICallForTransport transportable;

		public WaitForTransport(Actor self, Activity innerActivity)
		{
			transportable = self.TraitOrDefault<ICallForTransport>();
			QueueChild(self, innerActivity);
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (transportable != null)
				transportable.MovementCancelled(self);

			return NextActivity;
		}
	}
}
