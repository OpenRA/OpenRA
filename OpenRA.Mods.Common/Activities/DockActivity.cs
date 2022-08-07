#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class DockActivity : Activity
	{
		readonly IMove movement;
		readonly DockManager dockManager;
		readonly Actor self;
		readonly Aircraft aircraft;
		readonly INotifyDockableAction[] notifyDockActions;
		readonly ICallForTransport[] transportCallers;
		readonly BitSet<DockType> desiredTypes;
		Dock dock;

		public DockActivity(DockManager dockManager, IDockable desired = null, Dock dock = null)
			: this(dockManager, desired?.MyDockType ?? default, dock) { }

		public DockActivity(DockManager dockManager, BitSet<DockType> desired, Dock dock = null)
		{
			this.dockManager = dockManager;
			this.dock = dock;
			desiredTypes = desired;
			self = dockManager.Self;
			aircraft = self.TraitOrDefault<Aircraft>();
			movement = dockManager.Self.Trait<IMove>();
			notifyDockActions = self.TraitsImplementing<INotifyDockableAction>().ToArray();
			transportCallers = self.TraitsImplementing<ICallForTransport>().ToArray();
			Console.WriteLine("DockActivity called " + self.Info.Name + ":" + self.ActorID);
		}

		protected override void OnFirstRun(Actor self)
		{
			if (dock != null && dock.IsAliveAndInWorld)
				dockManager.LinkDock(dock);
		}

		public override bool Tick(Actor self)
		{
			// This ensures transports are also cancelled when the host becomes invalid
			if (!dockManager.IsAliveAndInWorld)
				Cancel(self, true);

			if (IsCanceling)
				return true;

			// Find the nearest best dock if not explicitly ordered to a specific dock
			if (dock == null || dockManager.LinkedDock == null)
				dock = dockManager.ChooseNewDock(null, desiredTypes);

			// No docks exist; check again after delay defined in dockable.
			if (dock == null || dockManager.LinkedDock == null)
			{
				var nearestDock = dockManager.ChooseNewDock(null, desiredTypes, true);
				if (nearestDock != null)
				{
					var nearestDelta = (nearestDock.Position - self.CenterPosition).LengthSquared;
					var dist = nearestDock.Info.WaitDistanceFromResupplyBase.Length;

					// If no dock is available, move near one and wait
					if (nearestDelta > dist)
					{
						var randomPosition = WVec.FromPDF(self.World.SharedRandom, 2) * dist / 1024;
						var target = Target.FromPos(nearestDock.Position + randomPosition);

						QueueChild(movement.MoveTo(nearestDock.Location, nearestDock.Info.CloseEnough < WDist.Zero ? 0 : nearestDock.Info.CloseEnough.Length, targetLineColor: dockManager.DockLineColor));
					}
					else
						QueueChild(new Wait(dockManager.Info.SearchForDockDelay));
				}
				else
					QueueChild(new Wait(dockManager.Info.SearchForDockDelay));

				return false;
			}

			bool isCloseEnough;

			// var delta = (dock.Position + (aircraft == null ? WVec.Zero : dock.Info.AircraftOffset) - self.CenterPosition).LengthSquared;
			var delta = (dock.Position - self.CenterPosition).LengthSquared;

			// Negative means there's no distance limit.
			if (dock.Info.CloseEnough < WDist.Zero)
				isCloseEnough = true;
			else
				isCloseEnough = self.Location == dock.Location;
				// isCloseEnough = delta <= dock.Info.CloseEnough.LengthSquared;

			if (!isCloseEnough)
			{
				foreach (var n in notifyDockActions)
					n.MovingToDock(dockManager, dock);

				// if (aircraft != null)
				// {
				// 	// QueueChild(movement.MoveWithinRange(Target.FromCell(self.World, dock.Location), WDist.Zero, targetLineColor: dockManager.DockLineColor));
				// 	QueueChild(new Land(self, Target.FromCell(self.World, dock.Location), dock.Info.AircraftOffset, dock.Info.Facing, dockManager.DockLineColor));
				// }
				// else
				QueueChild(movement.MoveTo(dock.Location, dock.Info.CloseEnough <= WDist.Zero ? 0 : dock.Info.CloseEnough.Length, targetLineColor: dockManager.DockLineColor));
				transportCallers.FirstOrDefault(t => t.MinimumDistance.LengthSquared < delta)?.RequestTransport(self, dock.Location);

				return false;
			}

			if (aircraft != null)
				QueueChild(new Land(self, Target.FromActor(dock.Self), dock.Info.Facing, dockManager.DockLineColor));

			QueueChild(new CallFunc(() => dock.DockedUnit = dockManager, false));
			if (self.TraitOrDefault<WithVoxelUnloadBody>() == null)
				QueueChild(new SpriteDockSequence(dockManager, dock));
			else
				QueueChild(new VoxelDockSequence(dockManager, dock));
			QueueChild(new CallFunc(() => dock.DockedUnit = null, false));
			return true;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			foreach (var n in notifyDockActions)
				n.MovementCancelled(dockManager);

			// HACK: force move activities to ignore the transit-only cells when cancelling
			// The idle handler will take over and move them into a safe cell
			if (ChildActivity != null)
				foreach (var c in ChildActivity.ActivitiesImplementing<Move>())
					c.Cancel(self, false, true);

			foreach (var t in transportCallers)
				t.MovementCancelled(self);

			base.Cancel(self, keepQueue);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (dock != null)
				yield return new TargetLineNode(Target.FromActor(dock.Self), dockManager.DockLineColor);
			else
				yield return new TargetLineNode(Target.FromActor(dockManager.LinkedDock?.Self), dockManager.DockLineColor);
		}
	}
}
