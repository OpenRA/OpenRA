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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class RepairDocking : Activity, IDockActivity
	{
		readonly Repairable repairable;

		public RepairDocking(Actor self, Actor host)
		{
			repairable = self.Trait<Repairable>();

			var dc = self.Trait<DockClient>();
			dc.Release(dc.CurrentDock);
		}

		public override Activity Tick(Actor self)
		{
			// No need for tick. This is a virtual activity just to fit IDockActivity.
			throw new NotImplementedException();
		}

		Activity IDockActivity.ActivitiesAfterDockDone(Actor host, Actor client, Dock dock)
		{
			return DockUtils.GenericFollowRallyPointActivities(host, client, dock, this);
		}

		Activity IDockActivity.ActivitiesOnDockFail(Actor client)
		{
			// stay idle
			return null;
		}

		Activity IDockActivity.ApproachDockActivities(Actor host, Actor client, Dock dock)
		{
			return DockUtils.GenericApproachDockActivities(host, client, dock, this, true);
		}

		Activity IDockActivity.DockActivities(Actor host, Actor client, Dock dock)
		{
			return repairable.AfterReachActivities(client, host, dock);
		}
	}
}
