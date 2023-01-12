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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public abstract class HarvesterDockSequence : Activity
	{
		protected enum DockingState { Wait, Turn, Drag, Dock, Loop, Undock, Complete }

		protected readonly Actor RefineryActor;
		protected readonly Refinery Refinery;
		protected readonly Harvester Harv;
		protected readonly WAngle DockAngle;
		protected readonly bool IsDragRequired;
		protected readonly WVec DragOffset;
		protected readonly int DragLength;
		protected readonly WPos StartDrag;
		protected readonly WPos EndDrag;

		protected DockingState dockingState;

		readonly INotifyDockClient[] notifyDockClients;
		readonly INotifyDockHost[] notifyDockHosts;

		public HarvesterDockSequence(Actor self, Actor refineryActor, Refinery refinery)
		{
			dockingState = DockingState.Turn;
			Refinery = refinery;
			RefineryActor = refineryActor;
			DockAngle = refinery.DeliveryAngle;
			IsDragRequired = refinery.IsDragRequired;
			DragOffset = refinery.DragOffset;
			DragLength = refinery.DragLength;
			Harv = self.Trait<Harvester>();
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

				case DockingState.Turn:
					dockingState = DockingState.Drag;
					QueueChild(new Turn(self, DockAngle));
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
						OnStateDock(self);
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
					OnStateUndock(self);
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

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromActor(RefineryActor);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(RefineryActor), Color.Green);
		}

		public abstract void OnStateDock(Actor self);

		public abstract void OnStateUndock(Actor self);

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
	}
}
