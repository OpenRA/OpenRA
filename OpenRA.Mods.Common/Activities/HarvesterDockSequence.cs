#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public abstract class HarvesterDockSequence : Activity
	{
		protected enum State { Wait, Turn, Dock, Loop, Undock, Complete }

		protected readonly Actor Refinery;
		protected readonly Harvester Harv;
		protected readonly int DockAngle;
		protected readonly bool IsDragRequired;
		protected readonly WVec DragOffset;
		protected readonly int DragLength;
		protected readonly WPos StartDrag;
		protected readonly WPos EndDrag;

		protected State dockingState;

		public HarvesterDockSequence(Actor self, Actor refinery, int dockAngle, bool isDragRequired, WVec dragOffset, int dragLength)
		{
			dockingState = State.Turn;
			Refinery = refinery;
			DockAngle = dockAngle;
			IsDragRequired = isDragRequired;
			DragOffset = dragOffset;
			DragLength = dragLength;
			Harv = self.Trait<Harvester>();
			StartDrag = self.CenterPosition;
			EndDrag = refinery.CenterPosition + DragOffset;
		}

		public override Activity Tick(Actor self)
		{
			switch (dockingState)
			{
				case State.Wait:
					return this;
				case State.Turn:
					dockingState = State.Dock;
					if (IsDragRequired)
						return ActivityUtils.SequenceActivities(new Turn(self, DockAngle), new Drag(self, StartDrag, EndDrag, DragLength), this);
					return ActivityUtils.SequenceActivities(new Turn(self, DockAngle), this);
				case State.Dock:
					if (Refinery.IsInWorld && !Refinery.IsDead)
						foreach (var nd in Refinery.TraitsImplementing<INotifyDocking>())
							nd.Docked(Refinery, self);
					return OnStateDock(self);
				case State.Loop:
					if (!Refinery.IsInWorld || Refinery.IsDead || Harv.TickUnload(self, Refinery))
						dockingState = State.Undock;
					return this;
				case State.Undock:
					return OnStateUndock(self);
				case State.Complete:
					if (Refinery.IsInWorld && !Refinery.IsDead)
						foreach (var nd in Refinery.TraitsImplementing<INotifyDocking>())
							nd.Undocked(Refinery, self);
					Harv.LastLinkedProc = Harv.LinkedProc;
					Harv.LinkProc(self, null);
					if (IsDragRequired)
						return ActivityUtils.SequenceActivities(new Drag(self, EndDrag, StartDrag, DragLength), NextActivity);
					return NextActivity;
			}

			throw new InvalidOperationException("Invalid harvester dock state");
		}

		public override void Cancel(Actor self)
		{
			dockingState = State.Undock;
			base.Cancel(self);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromActor(Refinery);
		}

		public abstract Activity OnStateDock(Actor self);

		public abstract Activity OnStateUndock(Actor self);
	}
}