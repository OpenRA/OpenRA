#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public abstract class HarvesterDockSequence : Activity
	{
		protected enum DockingState { Wait, Turn, Dock, Loop, Undock, Complete }

		protected readonly Actor Refinery;
		protected readonly Harvester Harv;
		protected readonly int DockAngle;
		protected readonly bool IsDragRequired;
		protected readonly WVec DragOffset;
		protected readonly int DragLength;
		protected readonly WPos StartDrag;
		protected readonly WPos EndDrag;

		protected DockingState dockingState;

		public HarvesterDockSequence(Actor self, Actor refinery, int dockAngle, bool isDragRequired, WVec dragOffset, int dragLength)
		{
			dockingState = DockingState.Turn;
			Refinery = refinery;
			DockAngle = dockAngle;
			IsDragRequired = isDragRequired;
			DragOffset = dragOffset;
			DragLength = dragLength;
			Harv = self.Trait<Harvester>();
			StartDrag = self.CenterPosition;
			EndDrag = refinery.CenterPosition + DragOffset;
		}

		public override bool Tick(Actor self)
		{
			switch (dockingState)
			{
				case DockingState.Wait:
					return false;

				case DockingState.Turn:
					dockingState = DockingState.Dock;
					QueueChild(new Turn(self, DockAngle));
					if (IsDragRequired)
						QueueChild(new Drag(self, StartDrag, EndDrag, DragLength));
					return false;

				case DockingState.Dock:
					if (Refinery.IsInWorld && !Refinery.IsDead)
						foreach (var nd in Refinery.TraitsImplementing<INotifyDocking>())
							nd.Docked(Refinery, self);

					OnStateDock(self);
					return false;

				case DockingState.Loop:
					if (!Refinery.IsInWorld || Refinery.IsDead || Harv.TickUnload(self, Refinery))
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Undock:
					OnStateUndock(self);
					return false;

				case DockingState.Complete:
					if (Refinery.IsInWorld && !Refinery.IsDead)
						foreach (var nd in Refinery.TraitsImplementing<INotifyDocking>())
							nd.Undocked(Refinery, self);

					Harv.LastLinkedProc = Harv.LinkedProc;
					Harv.LinkProc(self, null);
					if (IsDragRequired)
						QueueChild(new Drag(self, EndDrag, StartDrag, DragLength));

					return true;
			}

			throw new InvalidOperationException("Invalid harvester dock state");
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			dockingState = DockingState.Undock;
			base.Cancel(self);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromActor(Refinery);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(Refinery), Color.Green);
		}

		public abstract void OnStateDock(Actor self);

		public abstract void OnStateUndock(Actor self);
	}
}
