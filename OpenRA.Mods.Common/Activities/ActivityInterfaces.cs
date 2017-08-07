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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public interface IDockActivity
	{
		// How to command the actor to move to the dock location.
		// Assumes that this activity has 100% chance of making client to reach the dock.
		// That means if you make client to move with nearenough threshold, things will break
		// and the actor will perform docking actions out of place.
		Activity ApproachDockActivities(Actor host, Actor client, Dock dock);

		// What to do during the dock.
		// No need for death check on host, DockManager will automatically use ActivitiesOnDockFail.
		Activity DockActivities(Actor host, Actor client, Dock dock);

		// Called when docking is complete.
		// Queue client's post-undocking activities in this function, too.
		Activity ActivitiesAfterDockDone(Actor host, Actor client, Dock dock);

		// What to do on dock fail.
		// ... host and dock are gone. Only client is the valid parameter!
		Activity ActivitiesOnDockFail(Actor client);
	}
}
