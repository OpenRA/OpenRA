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
		protected readonly LinkClientManager LinkClient;
		protected readonly Actor LinkHostActor;
		protected readonly ILinkHost LinkHost;
		protected bool linkingStarted = false;
		protected readonly int CloseEnough;

		public ProximityLinkSequence(LinkClientManager client, Actor hostActor, ILinkHost host, int closeEnough)
		{
			LinkClient = client;
			LinkHost = host;
			LinkHostActor = hostActor;
			CloseEnough = closeEnough;
			QueueChild(new Wait(host.LinkWait));
		}

		bool HostAliveAndInRange(Actor self) =>
			!IsCanceling && !LinkHostActor.IsDead && LinkHostActor.IsInWorld
			&& (self.CenterPosition - LinkHost.LinkPosition).Length <= CloseEnough;

		protected override void OnFirstRun(Actor self)
		{
			if (HostAliveAndInRange(self) && LinkClient.CanLinkTo(LinkHostActor, LinkHost, false, true))
			{
				LinkHost.OnLinkStarted(LinkHostActor, self, LinkClient);
				foreach (var nd in self.TraitsImplementing<INotifyLinkClient>())
					nd.Linked(self, LinkHostActor);

				foreach (var nd in LinkHostActor.TraitsImplementing<INotifyLinkHost>())
					nd.Linked(LinkHostActor, self);

				linkingStarted = true;
			}
			else
				LinkClient.UnreserveHost();
		}

		public override bool Tick(Actor self)
		{
			if (!linkingStarted)
				return true;

			if (HostAliveAndInRange(self) && !LinkClient.OnLinkTick(self, LinkHostActor, LinkHost))
				return false;

			if (!LinkHostActor.IsDead)
			{
				LinkHost.OnLinkCompleted(LinkHostActor, self, LinkClient);
				foreach (var nd in LinkHostActor.TraitsImplementing<INotifyLinkHost>())
					nd.Unlinked(LinkHostActor, self);
			}

			foreach (var nd in self.TraitsImplementing<INotifyLinkClient>())
				nd.Unlinked(self, LinkHostActor);

			return true;
		}
	}
}
