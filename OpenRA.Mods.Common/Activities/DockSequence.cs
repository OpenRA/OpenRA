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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public abstract class DockSequence : Activity
	{
		protected enum DockingState { Wait, Turn, Drag, Dock, Loop, Undock, Complete }

		protected readonly Dock Dock;
		protected readonly DockManager DockManager;
		protected readonly Actor Self;
		protected readonly WAngle? DockFascing;
		protected readonly bool IsDragRequired;
		protected readonly WVec DragOffset;
		protected readonly int DragLength;
		protected readonly WPos StartDrag;
		protected readonly WPos EndDrag;

		readonly IMove move;
		readonly IMoveInfo moveInfo;

		protected DockingState dockingState;
		readonly bool stayOnResupplier;

		protected IEnumerable<IDockable> dockables;
		protected INotifyDock[] notifyDocks;
		protected INotifyDockable[] notifyDockables;

		bool successfulDock = false;

		public DockSequence(DockManager dockManager, Dock dock)
		{
			dockingState = DockingState.Turn;
			stayOnResupplier = !dockManager.Info.TakeOffOnResupply;
			Dock = dock;
			DockFascing = dock.Info.Facing;
			IsDragRequired = dock.Info.IsDragRequired;
			DragOffset = dock.Info.DragOffset;
			DragLength = dock.Info.DragLength;
			DockManager = dockManager;
			dockables = dockManager.AvailableDockables(dock);

			Self = dockManager.Self;
			StartDrag = Self.CenterPosition;
			EndDrag = dock.Self.CenterPosition + DragOffset;
			move = Self.Trait<IMove>();
			moveInfo = Self.Info.TraitInfo<IMoveInfo>();

			notifyDocks = dock.Self.TraitsImplementing<INotifyDock>().ToArray();
			notifyDockables = Self.TraitsImplementing<INotifyDockable>().ToArray();
			Console.WriteLine("DockSequence called " + Self.Info.Name + ":" + Self.ActorID);
		}

		public override bool Tick(Actor self)
		{
			switch (dockingState)
			{
				case DockingState.Wait:
					return false;

				case DockingState.Turn:
					dockingState = DockingState.Drag;
					if (DockFascing != null)
						QueueChild(new Turn(self, DockFascing ?? WAngle.Zero));

					return false;

				case DockingState.Drag:
					if (IsCanceling || !CanStillDock())
						return true;

					dockingState = DockingState.Dock;
					if (IsDragRequired)
						QueueChild(new Drag(self, StartDrag, EndDrag, DragLength));

					return false;

				case DockingState.Dock:
					if (!IsCanceling && CanStillDock())
						OnStateDock();
					else
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Loop:
					if (!IsCanceling || !CanStillDock())
					{
						dockables = dockables.Where(d => !d.TickDock(Dock));
						notifyDocks.Do(n => n.DockTick(DockManager, Dock));
						notifyDockables.Do(n => n.DockTick(DockManager, Dock));

						if (!dockables.Any())
							dockingState = DockingState.Undock;
					}
					else
						dockingState = DockingState.Undock;

					return false;

				case DockingState.Undock:
					OnStateUndock();
					return false;

				case DockingState.Complete:
					if (IsDragRequired)
						QueueChild(new Drag(self, EndDrag, StartDrag, DragLength));

					OnResupplyComplete(self);
					return true;
			}

			throw new InvalidOperationException("Invalid harvester dock state");
		}

		bool CanStillDock()
		{
			return dockables.Any(d => Dock.CanStillDock(d));
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromActor(Dock.Self);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(Dock.Self), Color.Green);
		}

		public virtual void OnStateDock()
		{
			notifyDocks.Do(n => n.Docked(DockManager, Dock));
			notifyDockables.Do(n => n.Docked(DockManager, Dock));
			DockManager.DockStarted(dockables, Dock);
		}

		public virtual void OnStateUndock()
		{
			dockingState = DockingState.Complete;
			successfulDock = true;
		}

		protected override void OnLastRun(Actor self)
		{
			Console.WriteLine("Last " + Self.Info.Name + ":" + Self.ActorID);
			OnResupplyEnding();
			base.OnLastRun(self);
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			Console.WriteLine("Cancel " + Self.Info.Name + ":" + Self.ActorID);
			OnResupplyEnding();
			base.Cancel(self, keepQueue);
		}

		void OnResupplyComplete(Actor self)
		{
			var isHostValid = CanStillDock();
			var rp = isHostValid ? Dock.Self.TraitOrDefault<RallyPoint>() : null;
			var aircraft = Self.TraitOrDefault<Aircraft>();
			if (aircraft != null)
			{
				if (successfulDock || !isHostValid || (!stayOnResupplier))
				{
					if ((self.CurrentActivity.NextActivity == null) && rp != null && rp.Path.Count > 0)
						foreach (var cell in rp.Path)
							QueueChild(new AttackMoveActivity(self, () => move.MoveTo(cell, 1, ignoreActor: GetActorBelow() ?? Dock.Self, targetLineColor: aircraft.Info.TargetLineColor)));
					else
						QueueChild(new TakeOff(self));
				}
			}
			else if (!stayOnResupplier && isHostValid)
			{
				// If we are on host, first leave host if the next activity is not a Move.
				if (self.CurrentActivity.NextActivity == null)
				{
					if (rp != null && rp.Path.Count > 0)
						foreach (var cell in rp.Path)
							QueueChild(new AttackMoveActivity(self, () => move.MoveTo(cell, 1, GetActorBelow() ?? Dock.Self, true, moveInfo.GetTargetLineColor())));
					else if (GetActorBelow() != null)
						QueueChild(move.MoveTo(Dock.Location));
				}
				else if (GetActorBelow() != null && !(self.CurrentActivity.NextActivity is Move))
					QueueChild(move.MoveTo(Dock.Location));
			}
		}

		Actor GetActorBelow()
		{
			return Self.World.ActorMap.GetActorsAt(Self.Location)
				.FirstOrDefault(a => a.Info.HasTraitInfo<RepairableInfo>());
		}

		void OnResupplyEnding()
		{
			notifyDocks.Do(n => n.Undocked(DockManager, Dock));
			notifyDockables.Do(n => n.Undocked(DockManager, Dock));
			DockManager.DockCompleted(Dock);
		}
	}
}
