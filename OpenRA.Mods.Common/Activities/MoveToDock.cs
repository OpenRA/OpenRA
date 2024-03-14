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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class MoveToDock : Activity
	{
		readonly LinkClientManager linkClient;
		Actor linkHostActor;
		ILinkHost linkHost;
		readonly INotifyLinkClientMoving[] notifylinkClientMoving;

		public MoveToDock(Actor self, Actor linkHostActor = null, ILinkHost linkHost = null)
		{
			linkClient = self.Trait<LinkClientManager>();
			this.linkHostActor = linkHostActor;
			this.linkHost = linkHost;
			notifylinkClientMoving = self.TraitsImplementing<INotifyLinkClientMoving>().ToArray();
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			if (linkClient.IsTraitDisabled)
			{
				Cancel(self, true);
				return true;
			}

			// Find the nearest LinkHost if not explicitly ordered to a specific host.
			if (linkHost == null || !linkHost.IsEnabledAndInWorld)
			{
				var host = linkClient.ClosestLinkHost(null);
				if (host.HasValue)
				{
					linkHost = host.Value.Trait;
					linkHostActor = host.Value.Actor;
				}
				else
				{
					// No hosts exist; check again after delay defined in linkClient.
					QueueChild(new Wait(linkClient.Info.SearchForLinkDelay));
					return false;
				}
			}

			if (linkClient.ReserveHost(linkHostActor, linkHost))
			{
				if (linkHost.QueueMoveActivity(this, linkHostActor, self, linkClient))
				{
					foreach (var ndcm in notifylinkClientMoving)
						ndcm.MovingToHost(self, linkHostActor, linkHost);

					return false;
				}

				linkHost.QueueLinkActivity(this, linkHostActor, self, linkClient);
				return true;
			}
			else
			{
				// The host explicitely chosen by the user is currently occupied. Wait and check again.
				QueueChild(new Wait(linkClient.Info.SearchForLinkDelay));
				return false;
			}
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			linkClient.UnreserveHost();
			foreach (var ndcm in notifylinkClientMoving)
				ndcm.MovementCancelled(self);

			base.Cancel(self, keepQueue);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (linkHostActor != null)
				yield return new TargetLineNode(Target.FromActor(linkHostActor), linkClient.LinkLineColor);
			else
			{
				if (linkClient.ReservedHostActor != null)
					yield return new TargetLineNode(Target.FromActor(linkClient.ReservedHostActor), linkClient.LinkLineColor);
			}
		}
	}
}
