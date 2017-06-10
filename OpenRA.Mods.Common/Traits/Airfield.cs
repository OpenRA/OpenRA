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

using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides docking procedures for helicopters")]
	class AirfieldInfo : ITraitInfo, Requires<DockManagerInfo>
	{
		public object Create(ActorInitializer init)
		{
			return new Airfield(init);
		}
	}

	public class Airfield : IAcceptDock
	{
		readonly Actor host;
		readonly DockManager dockManager;
		readonly RallyPoint rallyPoint;

		public Airfield(ActorInitializer init)
		{
			host = init.Self;
			dockManager = host.Trait<DockManager>();
			rallyPoint = host.TraitOrDefault<RallyPoint>();
		}

		void IAcceptDock.OnUndock(Actor client, Dock dock, Activity parameters)
		{
			client.SetTargetLine(Target.FromCell(client.World, rallyPoint.Location), Color.Green, false);

			// ResupplyAircraft handles this.
			// Take off and move to RP.
			if (rallyPoint != null)
			{
				client.QueueActivity(new Fly(client, Target.FromCell(client.World, rallyPoint.Location)));
				client.QueueActivity(new FlyCircle(client));
			}
		}

		void IAcceptDock.QueueDockActivity(Actor client, Dock dock, Activity parameters)
		{
			client.SetTargetLine(Target.FromCell(client.World, dock.Location), Color.Green, false);
			client.QueueActivity(new ResupplyAircraft(client));
		}

		Activity IAcceptDock.ApproachDockActivity(Actor client, Dock dock, Activity parameters)
		{
			// Let's reload. The assumption here is that for aircrafts, there are no waiting docks.
			System.Diagnostics.Debug.Assert(parameters is ReturnToBase, "Wrong parameter for landing");
			var rtb = parameters as ReturnToBase;
			return rtb.LandingProcedure(client, dock);
		}
	}
}
