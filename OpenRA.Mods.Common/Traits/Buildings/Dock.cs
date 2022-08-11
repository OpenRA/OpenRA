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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class DockInfo : TraitInfo
	{
		[Desc("Actual harvester facing when docking.")]
		public readonly WAngle DockAngle = WAngle.Zero;

		[Desc("Docking cell relative to top-left cell.")]
		public readonly CVec DockOffset = CVec.Zero;

		[Desc("Does the refinery require the harvester to be dragged in?")]
		public readonly bool IsDragRequired = false;

		[Desc("Vector by which the harvester will be dragged when docking.")]
		public readonly WVec DragOffset = WVec.Zero;

		[Desc("In how many steps to perform the dragging?")]
		public readonly int DragLength = 0;
	}

	// TODO: turn this into a trait
	public abstract class Dock : ITick, INotifySold, INotifyCapture, INotifyOwnerChanged, ISync, INotifyActorDisposing
	{
		protected readonly Actor Self;
		readonly DockInfo info;

		[Sync]
		Actor dockedHarv = null;

		[Sync]
		bool preventDock = false;

		public bool AllowDocking => !preventDock;
		public CVec DeliveryOffset => info.DockOffset;
		public WAngle DeliveryAngle => info.DockAngle;
		public bool IsDragRequired => info.IsDragRequired;
		public WVec DragOffset => info.DragOffset;
		public int DragLength => info.DragLength;

		public Dock(Actor self, DockInfo info)
		{
			Self = self;
			this.info = info;
		}

		public abstract Activity DockSequence(Actor harv, Actor self);

		public IEnumerable<TraitPair<Harvester>> GetLinkedHarvesters()
		{
			return Self.World.ActorsWithTrait<Harvester>().Where(a => a.Trait.LinkedProc == Self);
		}

		void CancelDock()
		{
			preventDock = true;
		}

		void ITick.Tick(Actor self)
		{
			// Harvester was killed while unloading
			if (dockedHarv != null && dockedHarv.IsDead)
				dockedHarv = null;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			CancelDock();
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);
		}

		public void OnDock(Actor harv, DeliverResources dockOrder)
		{
			if (!preventDock)
			{
				dockOrder.QueueChild(new CallFunc(() => dockedHarv = harv, false));
				dockOrder.QueueChild(DockSequence(harv, Self));
				dockOrder.QueueChild(new CallFunc(() => dockedHarv = null, false));
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			// Steal any docked harv too
			if (dockedHarv != null)
			{
				dockedHarv.ChangeOwner(newOwner);

				// Relink to this refinery
				dockedHarv.Trait<Harvester>().LinkProc(self);
			}
		}

		void INotifySold.Selling(Actor self) { CancelDock(); }
		void INotifySold.Sold(Actor self)
		{
			foreach (var harv in GetLinkedHarvesters())
				harv.Trait.UnlinkProc(harv.Actor, self);
		}
	}
}
