#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	public class ProximityLinkSequence : Activity
	{
		protected readonly DockClientManager LinkClient;
		protected readonly Actor LinkHostActor;
		protected readonly IDockHost LinkHost;
		protected bool linkingStarted = false;
		protected readonly int CloseEnough;

		public ProximityLinkSequence(DockClientManager client, Actor hostActor, IDockHost host, int closeEnough, int dockWait)
		{
			LinkClient = client;
			LinkHost = host;
			LinkHostActor = hostActor;
			CloseEnough = closeEnough;

			if (dockWait > 0)
				QueueChild(new Wait(dockWait));
		}

		bool HostAliveAndInRange(Actor self) =>
			!IsCanceling && !LinkHostActor.IsDead && LinkHostActor.IsInWorld
			&& (self.CenterPosition - LinkHost.DockPosition).Length <= CloseEnough;

		protected override void OnFirstRun(Actor self)
		{
			if (HostAliveAndInRange(self) && LinkClient.CanDockAt(LinkHostActor, LinkHost, false, true))
			{
				LinkHost.OnDockStarted(LinkHostActor, self, LinkClient);
				foreach (var nd in self.TraitsImplementing<INotifyDockClient>())
					nd.Docked(self, LinkHostActor);

				foreach (var nd in LinkHostActor.TraitsImplementing<INotifyDockHost>())
					nd.Docked(LinkHostActor, self);

				linkingStarted = true;
			}
			else
				LinkClient.UnreserveHost();
		}

		public override bool Tick(Actor self)
		{
			if (!linkingStarted)
				return true;

			if (HostAliveAndInRange(self) && !LinkClient.OnDockTick(self, LinkHostActor, LinkHost))
				return false;

			if (!LinkHostActor.IsDead)
			{
				LinkHost.OnDockCompleted(LinkHostActor, self, LinkClient);
				foreach (var nd in LinkHostActor.TraitsImplementing<INotifyDockHost>())
					nd.Undocked(LinkHostActor, self);
			}

			foreach (var nd in self.TraitsImplementing<INotifyDockClient>())
				nd.Undocked(self, LinkHostActor);

			return true;
		}
	}
}
