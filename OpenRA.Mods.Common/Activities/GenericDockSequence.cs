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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class GenericDockSequence : Activity
	{
		protected enum DockingState { Wait, Drag, Dock, Loop, Undock, Complete }

		protected readonly Actor LinkHostActor;
		protected readonly ILinkHost LinkHost;
		protected readonly WithDockingOverlay LinkHostSpriteOverlay;
		protected readonly LinkClientManager LinkClient;
		protected readonly IDockClientBody LinkClientBody;
		protected readonly bool IsDragRequired;
		protected readonly int DragLength;
		protected readonly WPos StartDrag;
		protected readonly WPos EndDrag;

		protected DockingState dockingState;

		readonly INotifyLinkClient[] notifyDockClients;
		readonly INotifyLinkHost[] notifyLinkHosts;

		bool dockInitiated = false;

		public GenericDockSequence(Actor self, LinkClientManager client, Actor hostActor, ILinkHost host)
		{
			dockingState = DockingState.Drag;

			LinkClient = client;
			LinkClientBody = self.TraitOrDefault<IDockClientBody>();
			notifyDockClients = self.TraitsImplementing<INotifyLinkClient>().ToArray();

			LinkHost = host;
			LinkHostActor = hostActor;
			LinkHostSpriteOverlay = hostActor.TraitOrDefault<WithDockingOverlay>();
			notifyLinkHosts = hostActor.TraitsImplementing<INotifyLinkHost>().ToArray();

			if (host is ILinkHostDrag sequence)
			{
				IsDragRequired = sequence.IsDragRequired;
				DragLength = sequence.DragLength;
				StartDrag = self.CenterPosition;
				EndDrag = hostActor.CenterPosition + sequence.DragOffset;
			}
			else
				IsDragRequired = false;

			QueueChild(new Wait(host.LinkWait));
		}

		public override bool Tick(Actor self)
		{
			switch (dockingState)
			{
				case DockingState.Wait:
					return false;

				case DockingState.Drag:
					if (IsCanceling || LinkHostActor.IsDead || !LinkHostActor.IsInWorld || !LinkClient.CanLinkTo(LinkHostActor, LinkHost, false, true))
					{
						LinkClient.UnreserveHost();
						return true;
					}

					dockingState = DockingState.Dock;
					if (IsDragRequired)
						QueueChild(new Drag(self, StartDrag, EndDrag, DragLength));

					return false;

				case DockingState.Dock:
					if (!IsCanceling && !LinkHostActor.IsDead && LinkHostActor.IsInWorld && LinkClient.CanLinkTo(LinkHostActor, LinkHost, false, true))
					{
						dockInitiated = true;
						PlayDockAnimations(self);
						LinkHost.OnLinkStarted(LinkHostActor, self, LinkClient);
						LinkClient.OnLinkStarted(self, LinkHostActor, LinkHost);
						NotifyDocked(self);
					}
					else
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Loop:
					if (IsCanceling || LinkHostActor.IsDead || !LinkHostActor.IsInWorld || LinkClient.OnLinkTick(self, LinkHostActor, LinkHost))
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Undock:
					if (dockInitiated)
						PlayUndockAnimations(self);
					else
						dockingState = DockingState.Complete;

					return false;

				case DockingState.Complete:
					LinkHost.OnLinkCompleted(LinkHostActor, self, LinkClient);
					LinkClient.OnLinkCompleted(self, LinkHostActor, LinkHost);
					NotifyUndocked(self);
					if (IsDragRequired)
						QueueChild(new Drag(self, EndDrag, StartDrag, DragLength));

					return true;
			}

			throw new InvalidOperationException("Invalid harvester dock state");
		}

		public virtual void PlayDockAnimations(Actor self)
		{
			PlayDockCientAnimation(self, () =>
			{
				if (LinkHostSpriteOverlay != null && !LinkHostSpriteOverlay.Visible)
				{
					dockingState = DockingState.Wait;
					LinkHostSpriteOverlay.Visible = true;
					LinkHostSpriteOverlay.WithOffset.Animation.PlayThen(LinkHostSpriteOverlay.Info.Sequence, () =>
					{
						dockingState = DockingState.Loop;
						LinkHostSpriteOverlay.Visible = false;
					});
				}
				else
					dockingState = DockingState.Loop;
			});
		}

		public virtual void PlayDockCientAnimation(Actor self, Action after)
		{
			if (LinkClientBody != null)
			{
				dockingState = DockingState.Wait;
				LinkClientBody.PlayDockAnimation(self, () => after());
			}
			else
				after();
		}

		public virtual void PlayUndockAnimations(Actor self)
		{
			if (LinkHostActor.IsInWorld && !LinkHostActor.IsDead && LinkHostSpriteOverlay != null && !LinkHostSpriteOverlay.Visible)
			{
				dockingState = DockingState.Wait;
				LinkHostSpriteOverlay.Visible = true;
				LinkHostSpriteOverlay.WithOffset.Animation.PlayBackwardsThen(LinkHostSpriteOverlay.Info.Sequence, () =>
				{
					PlayUndockClientAnimation(self, () =>
					{
						dockingState = DockingState.Complete;
						LinkHostSpriteOverlay.Visible = false;
					});
				});
			}
			else
				PlayUndockClientAnimation(self, () => dockingState = DockingState.Complete);
		}

		public virtual void PlayUndockClientAnimation(Actor self, Action after)
		{
			if (LinkClientBody != null)
			{
				dockingState = DockingState.Wait;
				LinkClientBody.PlayReverseDockAnimation(self, () => after());
			}
			else
				after();
		}

		void NotifyDocked(Actor self)
		{
			foreach (var nd in notifyDockClients)
				nd.Linked(self, LinkHostActor);

			foreach (var nd in notifyLinkHosts)
				nd.Linked(LinkHostActor, self);
		}

		void NotifyUndocked(Actor self)
		{
			foreach (var nd in notifyDockClients)
				nd.Unlinked(self, LinkHostActor);

			if (LinkHostActor.IsInWorld && !LinkHostActor.IsDead)
				foreach (var nd in notifyLinkHosts)
					nd.Unlinked(LinkHostActor, self);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromActor(LinkHostActor);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(LinkHostActor), Color.Green);
		}
	}
}
