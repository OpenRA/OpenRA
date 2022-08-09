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

		readonly INotifyDockable[] notifyDockables;
		readonly INotifyDock[] notifyDocks;

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
			notifyDockables = self.TraitsImplementing<INotifyDockable>().ToArray();
			notifyDocks = refineryActor.TraitsImplementing<INotifyDock>().ToArray();
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
					if (IsCanceling || !Refinery.IsEnabled || Harv.IsTraitDisabled)
						return true;

					dockingState = DockingState.Dock;
					if (IsDragRequired)
						QueueChild(new Drag(self, StartDrag, EndDrag, DragLength));

					return false;

				case DockingState.Dock:
					if (!IsCanceling && Refinery.IsEnabled && !Harv.IsTraitDisabled)
					{
						OnStateDock(self);
						NotifyDocked(self);
					}
					else
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Loop:
					if (IsCanceling || !Refinery.IsEnabled || Harv.IsTraitDisabled || Harv.TickUnload(self, RefineryActor))
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Undock:
					OnStateUndock(self);
					return false;

				case DockingState.Complete:
					NotifyUndocked(self);
					if (IsDragRequired)
						QueueChild(new Drag(self, EndDrag, StartDrag, DragLength));

					return true;
			}

			throw new InvalidOperationException("Invalid harvester dock state");
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			foreach (var nd in notifyDockables)
				nd.Canceled(self, RefineryActor);

			base.Cancel(self, keepQueue);
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
			foreach (var nd in notifyDockables)
				nd.Docked(self, RefineryActor);

			foreach (var dock in notifyDocks)
				dock.Docked(RefineryActor, self);
		}

		void NotifyUndocked(Actor self)
		{
			foreach (var nd in notifyDockables)
				nd.Undocked(self, RefineryActor);

			if (Refinery.IsEnabled)
				foreach (var nd in notifyDocks)
					nd.Undocked(RefineryActor, self);
		}
	}
}
