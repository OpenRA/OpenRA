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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public sealed class DockType { DockType() { } }

	public class DockInfo : ConditionalTraitInfo
	{
		[Desc("Docking type")]
		public readonly BitSet<DockType> Type;

		[Desc("Actual unit facing when docking. If not set a unit can dock at any angle")]
		public readonly WAngle? Facing;

		[Desc("Docking offset relative to the top left corner of the actor")]
		public readonly CVec Offset = CVec.Zero;

		[Desc("Docking offset for aricraft relative to Offset")]
		public readonly WVec AircraftOffset = new WVec(512, 512, 0);

		[Desc("How close a unit needs to be to be considered as docked. If set to -1 unit will dock")]
		public readonly WDist CloseEnough = WDist.Zero;

		[Desc("Does the dock require the unit to be dragged in?")]
		public readonly bool IsDragRequired = false;

		[Desc("Vector by which the unit will be dragged when docking.")]
		public readonly WVec DragOffset = WVec.Zero;

		[Desc("In how many steps to perform the dragging?")]
		public readonly int DragLength = 0;

		[Desc("Find a new structure to dock at if more than this many units are already waiting.")]
		public readonly int MaxOccupancy = 2;

		[Desc("The pathfinding cost penalty applied for each unit waiting to dock.")]
		public readonly int QueueCostModifier = 12;

		[Desc("The pathfinding cost penalty applied for docking at an allied structure.")]
		public readonly int AlliedCostModifier = 12;

		[Desc("The distance of the resupply base that the unit will wait for its turn.")]
		public readonly WDist WaitDistanceFromResupplyBase = new WDist(3072);

		[NotificationReference("Speech")]
		[Desc("Speech notification played when starting to repair a unit.")]
		public readonly string StartDockingNotification = null;

		[Desc("Text notification displayed when starting to repair a unit.")]
		public readonly string StartDockingTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Speech notification played when repairing a unit is done.")]
		public readonly string FinishDockingNotification = null;

		[Desc("Text notification displayed when repairing a unit is done.")]
		public readonly string FinishDockingTextNotification = null;
		public override object Create(ActorInitializer init) { return new Dock(init.Self, this); }
	}

	public class Dock : ConditionalTrait<DockInfo>, ITick, INotifySold, INotifyCapture,
		INotifyOwnerChanged, ISync, INotifyActorDisposing
	{
		public readonly Actor Self;

		public DockManager DockedUnit = null;
		readonly List<DockManager> reservedDockables = new List<DockManager>();

		public BitSet<DockType> MyDockType => Info.Type;
		public CPos Location => Self.Location + Info.Offset;
		public WPos Position => Self.World.Map.CenterOfCell(Location);

		public bool IsAliveAndInWorld => !IsTraitDisabled && !Self.IsDead && Self.IsInWorld && !Self.Disposed;
		public int Occupancy { get { RefreshOccupancy(); return reservedDockables.Count; } }

		public int Cost => Occupancy * Info.QueueCostModifier;

		public Dock(Actor self, DockInfo info)
			: base(info)
		{
			Self = self;
		}

		public bool IsUncoccupied()
		{
			RefreshOccupancy();
			return reservedDockables.Count < Info.MaxOccupancy;
		}

		public bool CanDock(IDockable dockable, bool allowedToForceEnter)
		{
			RefreshOccupancy();
			return CanStillDock(dockable) && (allowedToForceEnter || reservedDockables.Count < Info.MaxOccupancy || reservedDockables.Contains(dockable.DockManager));
		}

		public bool CanStillDock(IDockable dockable)
		{
			return IsAliveAndInWorld && dockable.IsAliveAndInWorld && dockable.Self.Owner.IsAlliedWith(Self.Owner);
		}

		public bool Reserve(DockManager dockManager)
		{
			RefreshOccupancy();
			if (reservedDockables.Count < Info.MaxOccupancy && !reservedDockables.Contains(dockManager))
			{
				reservedDockables.Add(dockManager);
				return true;
			}

			return false;
		}

		public void Unreserve(DockManager dockManager)
		{
			if (reservedDockables.Contains(dockManager))
			{
				dockManager.UnlinkProc(this);
				reservedDockables.Remove(dockManager);
			}
		}

		public void RefreshOccupancy()
		{
			reservedDockables.Where(h => !h.IsAliveAndInWorld).ToList().Do(h => Unreserve(h));
		}

		void ITick.Tick(Actor self)
		{
			// Unit was killed while docking
			if (DockedUnit != null && !DockedUnit.IsAliveAndInWorld)
				DockedUnit = null;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			while (reservedDockables.Count > 0)
				Unreserve(reservedDockables[0]);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			while (reservedDockables.Count > 0)
				Unreserve(reservedDockables[0]);
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			// Steal any docked unit too
			if (DockedUnit != null)
			{
				DockedUnit.Self.ChangeOwner(newOwner);

				// Relink to this dock
				DockedUnit.LinkDock(this);
			}
		}

		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self)
		{
			while (reservedDockables.Count > 0)
				Unreserve(reservedDockables[0]);
		}
	}
}
