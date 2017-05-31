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
using System;

namespace OpenRA.Mods.Common.Traits
{
	public class DockManagerInfo : ITraitInfo, Requires<DockInfo>
	{
		[Desc("Are any of the docks lie outside the building footprint? (and needs obstacle checking)")]
		public readonly bool ExternalDocks = false;

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
		readonly DockManagerInfo info;
		readonly Actor self; // == host
		readonly Dock[] allDocks;
		readonly Dock[] serviceDocks;
		readonly Dock[] waitDocks;
		readonly List<Actor> queue = new List<Actor>();

		CPos lastLocation; // in case this is a mobile dock.

		public bool HasExternalDock { get { return info.ExternalDocks; } }

		// for blocking check.
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

		// For determining whether PROC to play animation or not. (+ some others)
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

		public void ReserveDock(Actor self, Actor client, Activity postUndockActivity)
		{
			// First, put the new client in the queue then process it.
			if (!queue.Contains(client))
			{
				queue.Add(client);

				// Initialize this noob.
				// It might had been transferred from proc A to this proc.
				var dc = client.Trait<DockClient>();
				dc.PostUndockActivity = postUndockActivity;
				dc.DockState = DockState.NotAssigned;
			}

			// notify the queue
			processQueue(self, client);
		}

		// I'm assuming the docks.Length and queue size are small.
		// If it isn't the case then we need KilledNotification.
		void updateOccupierDeadOrAlive()
		{
			foreach (var d in allDocks)
			{
				var a = d.Occupier;
				if (a == null)
					continue;

				bool rm = false;
				if (a.Disposed || a.IsDead)
					rm = true;
				else if (a.Trait<Harvester>().LinkedProc != self)
					rm = true;
				else if (a.IsIdle)
					rm = true;
				// And there might be some intermediate states but that kind of case
				// won't happen for too long, because it implies losing game for the player or something.

				if (rm)
					OnUndock(a, d);
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
			// Currently nothing to do...?
		}

		public void OnUndock(Actor harv, Dock dock)
		{
			dock.Occupier = null;
			processQueue(self, null); // notify queue
		}

		void serveHead(Actor self, Actor head, Dock serviceDock)
		{
			var dockClient = head.Trait<DockClient>();
			var currentDock = dockClient.CurrentDock;

			// cd == null means the queue is not so busy that the head is a new comer. (for example, head == client case)
			// With thi in mind,
			// 4 cases of null/not nullness of dock and cd:
			// cd == null and dock == null    ERROR: What? Docks can't be busy when cd == null. Can't happen.
			// cd == null and dock != null    Safe to serve.
			// cd != null and dock == null    ERROR: First in line, has nowhere to go? Can't happen.
			// cd != null and dock != null    Was in the waiting queue and now ready to serve.
			// So, except for the errorneous state that can't happen, head is safe to serve.
			// We rule out dock == null case in outer loop, before calling this function though.

			// House keeping
			if (currentDock != null)
				currentDock.Occupier = null;
			dockClient.DockState = DockState.ServiceAssigned;
			dockClient.CurrentDock = serviceDock;
			serviceDock.Occupier = head;

			// Since there is 0 tolerance about distance, we WILL arrive at the dock (or we get stuck haha)
			head.QueueActivity(head.Trait<Mobile>().MoveTo(serviceDock.Location, 0));

			head.QueueActivity(new CallFunc(() => OnArrival(head, serviceDock)));

			// resource transfer activities are queued by OnDock.
			self.Trait<IAcceptDock>().QueueOnDockActivity(head, dockClient.PostUndockActivity, serviceDock);

			head.QueueActivity(new CallFunc(() => OnUndock(head, serviceDock)));

			// Move to south of the ref to avoid cluttering up with other dock locations
			head.QueueActivity(new Move(head, serviceDock.Location + serviceDock.Info.ExitOffset, new WDist(2048)));

			head.QueueActivity(dockClient.PostUndockActivity);
		}

		// As the actors are coming from all directions, first request, first served is not good.
		// Let it be first come first served.
		// We approximate distance computation by Rect-linear distance here, not Euclidean dist.
		Actor nearestClient(Actor self, IEnumerable<Actor> queue)
		{
			Actor r = null;
			int bestDist = -1;
			foreach (var a in queue)
			{
				var vec = self.World.Map.CenterOfCell(a.Location) - self.CenterPosition;
				var dist = vec.VerticalLength + vec.HorizontalLength;
				if (r == null || dist < bestDist)
				{
					r = a;
					bestDist = dist;
				}
			}
			return r;
		}

		// Get the queue going by popping queue.
		// Then, find a suitable dock place for the client and return it.
		void processQueue(Actor self, Actor client)
		{
			checkObstacle(self);

			updateOccupierDeadOrAlive();

			// Now serve the 1st in line, until all service docks are occupied.
			Actor head = null;
			while (queue.Count > 0)
			{
				head = nearestClient(self, queue);
				// find the first available slot in the service docks.
				var serviceDock = serviceDocks.FirstOrDefault(d => d.Occupier == null);
				if (serviceDock == null)
					break;
				serveHead(self, head, serviceDock);
				queue.Remove(head); // remove head
			}

			// was just a queue notification when the someone released the dock.
			if (client == null)
				return;

			// Is served already?
			if (!queue.Contains(client))
				return;

			var dockClient = client.Trait<DockClient>();

			Dock dock;
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
			dockClient.DockState = DockState.WaitAssigned;
			dockClient.CurrentDock = dock;
			dock.Occupier = client;

			// Cancel what ever it was doing and make harv come to the waiting dock.
			client.QueueActivity(client.Trait<Mobile>().MoveTo(dock.Location, 2));
			client.QueueActivity(new WaitFor(() => dockClient.DockState == DockState.ServiceAssigned));
		}
	}
}
