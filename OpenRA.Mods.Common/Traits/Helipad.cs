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
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides docking procedures for helicopters")]
	class HelipadInfo : ITraitInfo, Requires<DockManagerInfo>
	{
		public object Create(ActorInitializer init)
		{
			return new Helipad(init);
		}
	}

	public class Helipad : IAcceptDock
	{
		Actor host;
		DockManager dockManager;

		public Helipad(ActorInitializer init)
		{
			host = init.Self;
			dockManager = host.Trait<DockManager>();
		}

		// unused
		bool IAcceptDock.AllowDocking
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Unused
		IEnumerable<CPos> IAcceptDock.DockLocations
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Unused
		bool IAcceptDock.CanGiveResource(int amount)
		{
			throw new NotImplementedException();
		}

		// Unused
		void IAcceptDock.GiveResource(int amount)
		{
			throw new NotImplementedException();
		}

		void IAcceptDock.OnArrival(Actor client, Dock dock)
		{
			throw new NotImplementedException();
		}

		void IAcceptDock.OnUndock(Actor client, Dock dock)
		{	
			throw new NotImplementedException();
		}

		void IAcceptDock.QueueOnDockActivity(Actor client, Dock dock)
		{
			// Let's reload. The assumption here is that for aircrafts, there are no waiting docks.
			client.QueueActivity(ActivityUtils.SequenceActivities(
				new HeliFly(client, Target.FromPos(dock.CenterPosition)),
				new Turn(client, dock.Info.DockAngle),
				new HeliLand(client, false),
				new ResupplyAircraft(client)));

			// I know this depreciates AbortOnResupply activity but it is a bug to reuse NextActivity!
			//client.Info.TraitInfo<AircraftInfo>().AbortOnResupply ? null : client.CurrentActivity.NextActivity));
		}

		void IAcceptDock.QueueUndockActivity(Actor client, Dock dock)
		{
			// ResupplyAircraft handles this.
		}

		void IAcceptDock.ReserveDock(Actor client)
		{
			throw new NotImplementedException();
		}
	}
}
