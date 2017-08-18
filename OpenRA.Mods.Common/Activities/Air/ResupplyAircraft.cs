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

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class ResupplyAircraft : CompositeActivity
	{
		public ResupplyAircraft(Actor self) { }

		protected override void OnFirstRun(Actor self)
		{
			var aircraft = self.Trait<Aircraft>();
			var host = aircraft.GetActorBelow();

			if (host == null)
				return;

			var autoTakeOff = aircraft.Info.TakeOffOnResupply;

			if (aircraft.IsPlane)
			{
				// Planes should take off from their airfield immediately after resupplying (if TakeOffOnResupply is true).
				// HACK: Append NextInQueue to TakeOff to avoid moving to the Rallypoint (if NextInQueue is non-null).
				if (autoTakeOff)
					ChildActivity = ActivityUtils.SequenceActivities(
						aircraft.GetResupplyActivities(host)
						.Append(new AllowYieldingReservation(self))
						.Append(new TakeOff(self)).Append(NextInQueue)
						.ToArray());
				else
					ChildActivity = ActivityUtils.SequenceActivities(
						aircraft.GetResupplyActivities(host)
						.Append(new AllowYieldingReservation(self))
						.Append(new WaitFor(() => NextInQueue != null || aircraft.ReservedActor == null))
						.ToArray());
			}
			else
			{
				// Helicopters should take off from their helipad immediately after resupplying (if TakeOffOnResupply is true).
				// HACK: Append NextInQueue to TakeOff to avoid moving to the Rallypoint (if NextInQueue is non-null).
				if (autoTakeOff)
					ChildActivity = ActivityUtils.SequenceActivities(
						aircraft.GetResupplyActivities(host)
						.Append(new AllowYieldingReservation(self))
						.Append(new TakeOff(self)).Append(NextInQueue).ToArray());
				else
					ChildActivity = ActivityUtils.SequenceActivities(
						aircraft.GetResupplyActivities(host)
						.Append(new AllowYieldingReservation(self)).ToArray());
			}
		}

		public override Activity Tick(Actor self)
		{
			return NextActivity;
		}
	}
}
