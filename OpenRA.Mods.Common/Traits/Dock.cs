#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;

namespace OpenRA.Mods.Common.Traits
{
	public class DockInfo : ITraitInfo
	{
		[Desc("Docking offset relative to top-left cell. Can be used as WVec or CVec.",
			"*Use CPosOffset to \"cast\" this as CPos.")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Override Offset value and use center of the host actor as the dock offset?")]
		public readonly bool Center = true;

		[Desc("Just a waiting slot, not a dock that allows reloading / unloading / fixing")]
		public readonly bool WaitingPlace = false;

		[Desc("Dock angle. If < 0, the docker doesn't need to turn.")]
		public readonly int Angle = -1;

		[Desc("Does the refinery require the harvester to be dragged in?")]
		public readonly bool IsDragRequired = false;

		[Desc("Vector by which the harvester will be dragged when docking.")]
		public readonly WVec DragOffset = WVec.Zero;

		[Desc("In how many steps to perform the dragging?")]
		public readonly int DragLength = 0;

		[Desc("Priority of the docks, when managed by DockManager.")]
		public readonly int Order = 0;

		// "Cast" Offset as CVec.
		public CVec CPosOffset { get { return new CVec(Offset.X, Offset.Y); } }

		public object Create(ActorInitializer init) { return new Dock(init, this); }
	}

	public class Dock
	{
		public readonly DockInfo Info;
		public readonly Actor self;

		public Actor Occupier;

		// Returns the location of the dock, interpreting Offset as CVec.
		public CPos Location { get { return self.Location + Info.CPosOffset; } }

		// blocked by some immoble obstacle?
		public bool IsBlocked;

		public Dock(ActorInitializer init, DockInfo info)
		{
			Info = info;
			self = init.Self;
		}

		// Update IsBlocked on request...
		// Could have made IsBlocked a property but that will make the game slow.
		// Only checks for immobile objects so if there is a permanently EMP'ed mobile actor there,
		// then this will not work...
		public void CheckObstacle()
		{
			foreach (var a in self.World.ActorMap.GetActorsAt(Location))
				if (a != self && a.TraitOrDefault<Mobile>() == null)
				{
					IsBlocked = true;
					return;
				}
			IsBlocked = false;
		}
	}

	public class DockManagerInfo : ITraitInfo, Requires<DockInfo>
	{
		[Desc("Are any of the docks lie outside the building footprint? (and needs obstacle checking)")]
		public readonly bool ExternalDocks = true;

		[Desc("Queue will be processed this often.")]
		public readonly int WaitInterval = 19; // prime number yay

		public object Create(ActorInitializer init) { return new DockManager(init, this); }
	}

	/*
	 * The class to do all the crazy queue management.
	 * Not all multi-dock guys need queue management so making a separate manager class.
	 */
	public class DockManager
	{
		class DockEntry
		{
			public DeliverResources DockOrder;
			public Dock CurrentDock;
		}

		readonly DockManagerInfo info;
		readonly Actor self; // == proc
		readonly Dock[] allDocks;
		readonly Dock[] serviceDocks;
		readonly Dock[] waitDocks;
		readonly List<Actor> queue = new List<Actor>();
		readonly Dictionary<Actor, DockEntry> dockEntries = new Dictionary<Actor, DockEntry>();

		CPos lastLocation; // in case this is a mobile dock.

		public bool HasExternalDock { get { return info.ExternalDocks; } }

		public IEnumerable<CPos> DockLocations
		{
			get
			{
				foreach (var d in allDocks)
				{
					yield return d.Location;
				}
			}
		}

		public IEnumerable<Actor> DockedHarvs
		{
			get
			{
				foreach (var d in serviceDocks)
					if (d.Occupier != null && !d.Occupier.IsDead && !d.Occupier.Disposed)
						yield return d.Occupier;
			}
		}

		public DockManager(ActorInitializer init, DockManagerInfo info)
		{
			self = init.Self;
			this.info = info;

			// sort the dock traits by their Order trait.
			var t0 = self.TraitsImplementing<Dock>().ToList();
			t0.Sort(delegate (Dock a, Dock b) { return a.Info.Order - b.Info.Order; });
			var t1 = t0.Where(d => !d.Info.WaitingPlace).ToList();
			var t2 = t0.Where(d => d.Info.WaitingPlace).ToList();
			allDocks = t0.ToArray();
			serviceDocks = t1.ToArray();
			waitDocks = t2.ToArray();
		}

		void checkObstacle(Actor self)
		{
			if (self.Location == lastLocation)
				return;
			lastLocation = self.Location;

			foreach (var d in allDocks)
				d.CheckObstacle();
		}

		// onDock: actions to do when we ARRIVE at a service dock. (not waiting dock)
		// Not just one activity, as the activity may be the head activity + linked activity.
		// We modify guys activity when they get into waiting line so we keep pointers to onDock.
		//
		// If we run ot of dock, then return the last one and let clients wait near there.
		// So, the client will get a dock although it may share the dock with other client.
		//
		// The dock is assumed to be immobile at the time of this reservation task.
		// Even slave miners deploy to get dockings!
		public void ReserveDock(Actor self, Actor client, DeliverResources dockOrder)
		{
			// First, put the new client in the queue then process it.
			if (queue.Contains(client))
				return;

			queue.Add(client);
			dockEntries[client] = new DockEntry()
			{
				DockOrder = dockOrder
			};
			processQueue(self, client);
		}

		// I'm assuming the docks.Length and queue size are small.
		// If it isn't the case then we need KilledNotification.
		void updateOccupierDeadOrAlive()
		{
			foreach (var dok in allDocks)
				if (dok.Occupier != null && (dok.Occupier.Disposed || dok.Occupier.IsDead))
				{
					if (dockEntries.ContainsKey(dok.Occupier))
						dockEntries.Remove(dok.Occupier);
					dok.Occupier = null;
				}
		}

		public void CancelDock()
		{
			/*
			// Cancel the dock sequence
			if (dockedHarv != null && !dockedHarv.IsDead)
				dockedHarv.CancelActivity();

			foreach (var harv in virtuallyDockedHarvs)
			{
				if (!harv.IsDead)
					harv.CancelActivity();
			}
			*/
			Game.Debug("not impl");
		}

		public void OnArrival(Actor harv, Dock dock)
		{
			queue.Remove(harv);
		}

		public void OnUndock(Actor harv, Dock dock)
		{
			dock.Occupier = null;
			dockEntries.Remove(harv);
			processQueue(self, null); // notify queue
		}

		// Get the queue going by popping queue.
		// Then, find a suitable dock place for the client and return it.
		void processQueue(Actor self, Actor client)
		{
			if (queue.Count == 0)
				return;

			checkObstacle(self);

			updateOccupierDeadOrAlive();

			// Now serve the 1st in line.
			var head = queue.First();
			var entry = dockEntries[head];

			// find the first available slot in the service docks.
			var dock = serviceDocks.FirstOrDefault(d => d.Occupier == null);
			if (dock != null)
			{
				if (entry.CurrentDock != null)
					// Free the old slot, if we were there.
					entry.CurrentDock.Occupier = null;
				entry.CurrentDock = dock;

				// Let this guy continue with OnDock.
				dock.Occupier = head;
				var iao = self.Trait<IAcceptResources>();
				head.CancelActivity();
				head.QueueActivity(head.Trait<Mobile>().MoveTo(dock.Location, 0));
				// Since there is 0 tolerance about distance, we WILL arrive at the dock (or we get bug haha)
				iao.QueueOnDockActivity(head, entry.DockOrder, dock); // resource transfer activities are queued by OnDock.
			}

			// was just a queue notification when the head released the dock.
			if (client == null)
				return;

			// And as for our new client...
			if (client == head)
				return;

			entry = dockEntries[client];

			if (waitDocks.Count() == 0)
				dock = serviceDocks.Last();
			else
			{
				// Find any available waiting slot.
				dock = waitDocks.FirstOrDefault(d => d.Occupier == null);

				// on nothing, share the last slot.
				if (dock == null)
					dock = waitDocks.Last();
			}

			// For last dock, current dock and occupier will be messed up but doesn't matter.
			// The last one is shared anyway. The vacancy info is not very meaningful there.
			entry.CurrentDock = dock;
			dock.Occupier = client;

			// Cancel what ever it was doing and make harv come to the waiting dock.
			client.QueueActivity(false, client.Trait<Mobile>().MoveTo(dock.Location, 2));
		}
	}
}
