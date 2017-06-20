#region Copyright & License Information
/*
 * Modded by Boolbada of OP mod, from Engineer repair enter activity.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/* Works with no base engine modification */

namespace OpenRA.Mods.Yupgi_alert.Activities
{
	class EnterTransportWithWait : Activity
	{
		readonly Actor target;
		readonly Passenger passenger;
		readonly Cargo cargo;

		public EnterTransportWithWait(Actor self, Actor target)
		{
			this.target = target;
			passenger = self.Trait<Passenger>();
			cargo = target.Trait<Cargo>();
		}

		protected override void OnFirstRun(Actor self)
		{
			QueueChild(new MoveAdjacentTo(self, Target.FromActor(target)));
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			if (IsCanceled)
				return NextActivity;

			if (!passenger.Reserve(self, cargo))
			{
				QueueChild(new Wait(23));
				return this;
			}

			Queue(new EnterTransport(self, target));
			return NextActivity;
		}
	}
}
