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

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class ResupplyAircraft : Activity
	{
		public ResupplyAircraft(Actor self) { }

		protected override void OnFirstRun(Actor self)
		{
			var aircraft = self.Trait<Aircraft>();
			var host = aircraft.GetActorBelow();

			if (host == null)
				return;

			if (!aircraft.Info.TakeOffOnResupply)
			{
				ChildActivity = ActivityUtils.SequenceActivities(self, aircraft.GetResupplyActivities(host).ToArray());
				QueueChild(self, new AllowYieldingReservation(self));
				QueueChild(self, new WaitFor(() => NextInQueue != null || aircraft.ReservedActor == null));
			}
			else
			{
				ChildActivity = ActivityUtils.SequenceActivities(self, aircraft.GetResupplyActivities(host).ToArray());
				QueueChild(self, new AllowYieldingReservation(self));
				QueueChild(self, new TakeOff(self, (a, b, c) => NextInQueue == null && b.NextInQueue == null));
			}
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			// Conditional fixes being able to stop aircraft from resupplying.
			if (IsCanceling && NextInQueue == null)
				return new ResupplyAircraft(self);

			return NextActivity;
		}
	}
}
