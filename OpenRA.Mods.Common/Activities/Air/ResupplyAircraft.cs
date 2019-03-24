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

			QueueChild(self, new Resupply(self, host, WDist.Zero));
			QueueChild(self, new AllowYieldingReservation(self));
			if (aircraft.Info.TakeOffOnResupply)
				QueueChild(self, new TakeOff(self, (a, b, c) => NextActivity == null && b.NextActivity == null));
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			return NextActivity;
		}
	}
}
