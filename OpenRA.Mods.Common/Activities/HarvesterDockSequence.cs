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
		enum DockingState { Wait, Turn, Drag, Dock, Loop, Undock, Complete }

		readonly Actor refineryActor;
		readonly Refinery refinery;
		readonly Harvester harv;
		readonly WAngle dockAngle;
		readonly bool isDragRequired;
		readonly WVec dragOffset;
		readonly int dragLength;
		readonly WPos startDrag;
		readonly WPos endDrag;

		DockingState dockingState;

		readonly WithSpriteBody wsb;
		readonly WithDockingAnimationInfo wda;
		readonly WithDockingOverlay spriteOverlay;

		readonly INotifyDockable[] notifyDockables;
		readonly INotifyDock[] notifyDocks;
		bool dockAnimPlayed;

		public HarvesterDockSequence(Actor self, Actor refineryActor, Refinery refinery)
		{
			dockingState = DockingState.Turn;
			this.refinery = refinery;
			this.refineryActor = refineryActor;
			dockAngle = refinery.DeliveryAngle;
			isDragRequired = refinery.IsDragRequired;
			dragOffset = refinery.DragOffset;
			dragLength = refinery.DragLength;
			harv = self.Trait<Harvester>();
			startDrag = self.CenterPosition;
			endDrag = refineryActor.CenterPosition + dragOffset;

			wsb = self.TraitOrDefault<WithSpriteBody>();
			wda = self.Info.TraitInfoOrDefault<WithDockingAnimationInfo>();
			spriteOverlay = refineryActor.TraitOrDefault<WithDockingOverlay>();

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
					QueueChild(new Turn(self, dockAngle));
					return false;

				case DockingState.Drag:
					if (IsCanceling || !refinery.IsEnabled || harv.IsTraitDisabled)
						return true;

					dockingState = DockingState.Dock;
					if (isDragRequired)
						QueueChild(new Drag(self, startDrag, endDrag, dragLength));

					return false;

				case DockingState.Dock:
					if (!IsCanceling && refinery.IsEnabled && !harv.IsTraitDisabled)
					{
						OnStateDock(self);
						NotifyDocked(self);
					}
					else
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Loop:
					if (IsCanceling || !refinery.IsEnabled || harv.IsTraitDisabled || harv.TickUnload(self, refineryActor))
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Undock:
					OnStateUndock(self);
					return false;

				case DockingState.Complete:
					NotifyUndocked(self);
					if (isDragRequired)
						QueueChild(new Drag(self, endDrag, startDrag, dragLength));

					return true;
			}

			throw new InvalidOperationException("Invalid harvester dock state");
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			foreach (var nd in notifyDockables)
				nd.Canceled(self, refineryActor);

			base.Cancel(self, keepQueue);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromActor(refineryActor);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(refineryActor), Color.Green);
		}

		void OnStateDock(Actor self)
		{
			if (spriteOverlay != null && !spriteOverlay.Visible)
			{
				spriteOverlay.Visible = true;
				spriteOverlay.WithOffset.Animation.PlayThen(spriteOverlay.Info.Sequence, () =>
				{
					OnDock(self);
					spriteOverlay.Visible = false;
				});
			}
			else
				OnDock(self);
		}

		void OnDock(Actor self)
		{
			dockAnimPlayed = true;
			if (wsb != null && wda != null)
				wsb.PlayCustomAnimation(self, wda.DockSequence, () => wsb.PlayCustomAnimationRepeating(self, wda.DockLoopSequence));

			dockingState = DockingState.Loop;
		}

		void NotifyDocked(Actor self)
		{
			foreach (var nd in notifyDockables)
				nd.Docked(self, refineryActor);

			foreach (var dock in notifyDocks)
				dock.Docked(refineryActor, self);
		}

		void OnStateUndock(Actor self)
		{
			// If dock animation hasn't played, we didn't actually dock and have to skip the undock anim and notification
			if (!dockAnimPlayed)
			{
				Undock(self);
				return;
			}

			dockingState = DockingState.Wait;

			if (refinery.IsEnabled && spriteOverlay != null && !spriteOverlay.Visible)
			{
				dockingState = DockingState.Wait;
				spriteOverlay.Visible = true;
				spriteOverlay.WithOffset.Animation.PlayBackwardsThen(spriteOverlay.Info.Sequence, () =>
				{
					Undock(self);
					spriteOverlay.Visible = false;
				});
			}
			else
			{
				Undock(self);
			}
		}

		void Undock(Actor self)
		{
			if (wsb != null && wda != null)
				wsb.PlayCustomAnimationBackwards(self, wda.DockSequence, () => dockingState = DockingState.Complete);
			else
				dockingState = DockingState.Complete;
		}

		void NotifyUndocked(Actor self)
		{
			foreach (var nd in notifyDockables)
				nd.Undocked(self, refineryActor);

			if (refinery.IsEnabled)
				foreach (var nd in notifyDocks)
					nd.Undocked(refineryActor, self);
		}
	}
}
