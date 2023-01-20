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
	public class HarvesterDockSequence : Activity
	{
		protected enum DockingState { Wait, Drag, Dock, Loop, Undock, Complete }

		protected readonly Actor RefineryActor;
		protected readonly WithDockingOverlay DockHostSpriteOverlay;
		protected readonly Harvester Harv;
		protected readonly IDockClientBody DockClientBody;
		protected readonly bool IsDragRequired;
		protected readonly WVec DragOffset;
		protected readonly int DragLength;
		protected readonly WPos StartDrag;
		protected readonly WPos EndDrag;

		protected DockingState dockingState;

		readonly INotifyDockClient[] notifyDockClients;
		readonly INotifyDockHost[] notifyDockHosts;

		bool dockInitiated = false;

		public HarvesterDockSequence(Actor self, Actor refineryActor, Refinery refinery)
		{
			dockingState = DockingState.Drag;
			RefineryActor = refineryActor;
			DockHostSpriteOverlay = refineryActor.TraitOrDefault<WithDockingOverlay>();
			IsDragRequired = refinery.IsDragRequired;
			DragOffset = refinery.DragOffset;
			DragLength = refinery.DragLength;
			Harv = self.Trait<Harvester>();
			DockClientBody = self.TraitOrDefault<IDockClientBody>();
			StartDrag = self.CenterPosition;
			EndDrag = refineryActor.CenterPosition + DragOffset;
			notifyDockClients = self.TraitsImplementing<INotifyDockClient>().ToArray();
			notifyDockHosts = refineryActor.TraitsImplementing<INotifyDockHost>().ToArray();
		}

		public override bool Tick(Actor self)
		{
			switch (dockingState)
			{
				case DockingState.Wait:
					return false;

				case DockingState.Drag:
					if (IsCanceling || !RefineryActor.IsInWorld || RefineryActor.IsDead || Harv.IsTraitDisabled)
						return true;

					dockingState = DockingState.Dock;
					if (IsDragRequired)
						QueueChild(new Drag(self, StartDrag, EndDrag, DragLength));

					return false;

				case DockingState.Dock:
					if (!IsCanceling && RefineryActor.IsInWorld && !RefineryActor.IsDead && !Harv.IsTraitDisabled)
					{
						dockInitiated = true;
						PlayDockAnimations(self);
						NotifyDocked(self);
					}
					else
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Loop:
					if (IsCanceling || !RefineryActor.IsInWorld || RefineryActor.IsDead || Harv.IsTraitDisabled || Harv.TickUnload(self, RefineryActor))
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Undock:
					if (dockInitiated)
						PlayUndockAnimations(self);
					else
						dockingState = DockingState.Complete;

					return false;

				case DockingState.Complete:
					Harv.LastLinkedProc = Harv.LinkedProc;
					Harv.LinkProc(null);
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
			if (RefineryActor.IsInWorld && !RefineryActor.IsDead && DockHostSpriteOverlay != null && !DockHostSpriteOverlay.Visible)
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
				nd.Docked(self, RefineryActor);

			foreach (var nd in notifyDockHosts)
				nd.Docked(RefineryActor, self);
		}

		void NotifyUndocked(Actor self)
		{
			foreach (var nd in notifyDockClients)
				nd.Undocked(self, RefineryActor);

			if (RefineryActor.IsInWorld && !RefineryActor.IsDead)
				foreach (var nd in notifyDockHosts)
					nd.Undocked(RefineryActor, self);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromActor(RefineryActor);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(RefineryActor), Color.Green);
		}
	}
}
