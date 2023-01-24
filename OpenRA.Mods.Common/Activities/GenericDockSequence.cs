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

		protected readonly Actor DockHostActor;
		protected readonly IDockHost DockHost;
		protected readonly WithDockingOverlay DockHostSpriteOverlay;
		protected readonly DockClientManager DockClient;
		protected readonly IDockClientBody DockClientBody;
		protected readonly bool IsDragRequired;
		protected readonly int DragLength;
		protected readonly WPos StartDrag;
		protected readonly WPos EndDrag;

		protected DockingState dockingState;

		readonly INotifyDockClient[] notifyDockClients;
		readonly INotifyDockHost[] notifyDockHosts;

		bool dockInitiated = false;

		public GenericDockSequence(Actor self, DockClientManager client, Actor hostActor, IDockHost host)
		{
			dockingState = DockingState.Drag;

			DockClient = client;
			DockClientBody = self.TraitOrDefault<IDockClientBody>();
			notifyDockClients = self.TraitsImplementing<INotifyDockClient>().ToArray();

			DockHost = host;
			DockHostActor = hostActor;
			DockHostSpriteOverlay = hostActor.TraitOrDefault<WithDockingOverlay>();
			notifyDockHosts = hostActor.TraitsImplementing<INotifyDockHost>().ToArray();

			if (host is IDockHostDrag sequence)
			{
				IsDragRequired = sequence.IsDragRequired;
				DragLength = sequence.DragLength;
				StartDrag = self.CenterPosition;
				EndDrag = hostActor.CenterPosition + sequence.DragOffset;
			}
			else
				IsDragRequired = false;

			QueueChild(new Wait(host.DockWait));
		}

		public override bool Tick(Actor self)
		{
			switch (dockingState)
			{
				case DockingState.Wait:
					return false;

				case DockingState.Drag:
					if (IsCanceling || DockHostActor.IsDead || !DockHostActor.IsInWorld || !DockClient.CanDockAt(DockHostActor, DockHost, false, true))
					{
						DockClient.UnreserveHost();
						return true;
					}

					dockingState = DockingState.Dock;
					if (IsDragRequired)
						QueueChild(new Drag(self, StartDrag, EndDrag, DragLength));

					return false;

				case DockingState.Dock:
					if (!IsCanceling && !DockHostActor.IsDead && DockHostActor.IsInWorld && DockClient.CanDockAt(DockHostActor, DockHost, false, true))
					{
						dockInitiated = true;
						PlayDockAnimations(self);
						DockHost.OnDockStarted(DockHostActor, self, DockClient);
						DockClient.OnDockStarted(self, DockHostActor, DockHost);
						NotifyDocked(self);
					}
					else
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Loop:
					if (IsCanceling || DockHostActor.IsDead || !DockHostActor.IsInWorld || DockClient.OnDockTick(self, DockHostActor, DockHost))
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Undock:
					if (dockInitiated)
						PlayUndockAnimations(self);
					else
						dockingState = DockingState.Complete;

					return false;

				case DockingState.Complete:
					DockHost.OnDockCompleted(DockHostActor, self, DockClient);
					DockClient.OnDockCompleted(self, DockHostActor, DockHost);
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
				if (DockHostSpriteOverlay != null && !DockHostSpriteOverlay.Visible)
				{
					dockingState = DockingState.Wait;
					DockHostSpriteOverlay.Visible = true;
					DockHostSpriteOverlay.WithOffset.Animation.PlayThen(DockHostSpriteOverlay.Info.Sequence, () =>
					{
						dockingState = DockingState.Loop;
						DockHostSpriteOverlay.Visible = false;
					});
				}
				else
					dockingState = DockingState.Loop;
			});
		}

		public virtual void PlayDockCientAnimation(Actor self, Action after)
		{
			if (DockClientBody != null)
			{
				dockingState = DockingState.Wait;
				DockClientBody.PlayDockAnimation(self, () => after());
			}
			else
				after();
		}

		public virtual void PlayUndockAnimations(Actor self)
		{
			if (DockHostActor.IsInWorld && !DockHostActor.IsDead && DockHostSpriteOverlay != null && !DockHostSpriteOverlay.Visible)
			{
				dockingState = DockingState.Wait;
				DockHostSpriteOverlay.Visible = true;
				DockHostSpriteOverlay.WithOffset.Animation.PlayBackwardsThen(DockHostSpriteOverlay.Info.Sequence, () =>
				{
					PlayUndockClientAnimation(self, () =>
					{
						dockingState = DockingState.Complete;
						DockHostSpriteOverlay.Visible = false;
					});
				});
			}
			else
				PlayUndockClientAnimation(self, () => dockingState = DockingState.Complete);
		}

		public virtual void PlayUndockClientAnimation(Actor self, Action after)
		{
			if (DockClientBody != null)
			{
				dockingState = DockingState.Wait;
				DockClientBody.PlayReverseDockAnimation(self, () => after());
			}
			else
				after();
		}

		void NotifyDocked(Actor self)
		{
			foreach (var nd in notifyDockClients)
				nd.Docked(self, DockHostActor);

			foreach (var nd in notifyDockHosts)
				nd.Docked(DockHostActor, self);
		}

		void NotifyUndocked(Actor self)
		{
			foreach (var nd in notifyDockClients)
				nd.Undocked(self, DockHostActor);

			if (DockHostActor.IsInWorld && !DockHostActor.IsDead)
				foreach (var nd in notifyDockHosts)
					nd.Undocked(DockHostActor, self);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromActor(DockHostActor);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(DockHostActor), Color.Green);
		}
	}
}
