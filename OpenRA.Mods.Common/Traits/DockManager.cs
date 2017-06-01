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

		[Desc("Enable deadlock detection")]
		public readonly bool DetectDetectionEnabled = true;

		[Desc("Dead lock detection sampling is done this often.")]
		public readonly int DeadlockDetectionPeriod = 79; // prime number yay

		[Desc("Sampled this many times then the queue is reset.")]
		public readonly int DeadlockDetectionThreshold = 5;

		public object Create(ActorInitializer init) { return new DockManager(init, this); }
	}

	/*
	 * The class to do all the crazy queue management.
	 * Not all multi-dock guys need queue management so making a separate manager class.
	 */
	public class DockManager : ITick
	{
		readonly DockManagerInfo info;
		readonly Actor self; // == host

		readonly Dock[] allDocks;
		readonly Dock[] serviceDocks;
		readonly Dock[] waitDocks;
		readonly List<Actor> queue = new List<Actor>();

		CPos lastLocation; // in case this is a mobile dock.
		int ticks;

		public DockManager(ActorInitializer init, DockManagerInfo info)
		{
			self = init.Self;
			this.info = info;
			ticks = info.DeadlockDetectionPeriod;

			// sort the dock traits by their Order trait.
			var t0 = self.TraitsImplementing<Dock>().ToList();
			t0.Sort(delegate (Dock a, Dock b) { return a.Info.Order - b.Info.Order; });
			var t1 = t0.Where(d => !d.Info.WaitingPlace).ToList();
			var t2 = t0.Where(d => d.Info.WaitingPlace).ToList();
			allDocks = t0.ToArray();
			serviceDocks = t1.ToArray();
			waitDocks = t2.ToArray();
		}

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
				return serviceDocks.Where(d => d.Occupier != null).Select(d => d.Occupier);
			}
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

		void CancelDock(IEnumerable<Actor> actors)
		{
			foreach (var a in actors)
			{
				if (a != null && !a.IsDead)
				{
					a.CancelActivity();

					// A little bit of hard coding here.
					// Continue with post dock operation?
					if (a.TraitOrDefault<Harvester>() != null)
					{
						a.QueueActivity(new FindResources(a));
					}
				}
			}
		}

		// When the host dies...
		public void CancelDock()
		{
			CancelDock(serviceDocks.Select(d => d.Occupier));
			CancelDock(queue);
		}

		public void OnArrival(Actor client, Dock dock)
		{
			// We have "arrived". But did we get to where we intended?
			var dc = client.Trait<DockClient>();

			// I tried to arrive at a waiting spot but actually it was a working dock and I'm sitting on it!
			// (happens often when only one dock which is shared)
			if (dock.Info.WaitingPlace == false && dc.DockState == DockState.WaitAssigned && self.Location == dock.Location)
			{
				dc.Release(dock);
				self.CancelActivity();
				processQueue(self, client);
			}
		}

		int unchangedCount = 0;
		Actor[] deadlockTracker;
		void removeDeadLock(List<Actor> queue)
		{
			// This one is tricky.
			// When there are more than one service dock... say we have 2.
			// Client on x tries to dock at y and there is another client on y which tries to dock at x,
			// and when the dock is adjacent, we are in dead lock situation. They will not budge at all.
			// 1. Computing and finding cycles to break them is too expensive.
			// 2. Exploit that only adjacent docks can be deadlocked... Still too expensive. O(n^2).
			// 3. Assume dock is "linearly" placed and perform linear check: may not hold up.
			// Lets do simple sampling method here instead.

			// Queue is so sparse that everything is null.
			if (deadlockTracker == null || serviceDocks.All(d => d.Occupier == null))
			{
				unchangedCount = 0;
				deadlockTracker = serviceDocks.Select(d => d.Occupier).ToArray();
				return;
			}

			for (int i = 0; i < serviceDocks.Length; i++)
			{
				if (deadlockTracker[i] != serviceDocks[i].Occupier)
				{
					// reset counter and remember current composition.
					unchangedCount = 0;
					deadlockTracker = serviceDocks.Select(d => d.Occupier).ToArray();
					return;
				}
			}

			// no change!
			if (unchangedCount++ < info.DeadlockDetectionThreshold)
				return;

			// We have deadlock. Release these docks
			foreach (var d in serviceDocks)
				if (d.Occupier != null)
				{
					// The ones that occupy the docks are previously dequeued and can't be seen by processQueue yet.
					// Must put them back in the queue!
					queue.Add(d.Occupier);
					d.Occupier.CancelActivity();
					d.Occupier.Trait<DockClient>().Release(d);
				}

			unchangedCount = 0;
			processQueue(self, null);
		}

		public void OnUndock(Actor client, Dock dock)
		{
			client.Trait<DockClient>().Release(dock);
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

			dockClient.Release(currentDock);
			dockClient.Acquire(serviceDock, DockState.ServiceAssigned);

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
		Actor nearestClient(Actor self, Dock dock, IEnumerable<Actor> queue)
		{
			Actor r = null;
			int bestDist = -1;
			foreach (var a in queue)
			{
				var vec = a.Location - dock.Location;
				var dist = Math.Abs(vec.X) + Math.Abs(vec.Y);
				if (r == null || dist < bestDist)
				{
					r = a;
					bestDist = dist;
				}
			}
			return r;
		}

		void serveNewClient(Actor client)
		{
			var dockClient = client.Trait<DockClient>();

			Dock dock = null;
			if (waitDocks.Count() == 0)
			{
				dock = serviceDocks.Last();
			}
			else
			{
				// Find any available waiting slot.
				dock = waitDocks.FirstOrDefault(d => d.Occupier == null && !d.IsBlocked);

				// on nothing, share the last slot.
				if (dock == null)
					dock = waitDocks.Last();
			}

			// For last dock, current dock and occupier will be messed up but doesn't matter.
			// The last one is shared anyway. The vacancy info is not very meaningful there.
			if (dockClient.CurrentDock != null)
				dockClient.Release(dockClient.CurrentDock);
			dockClient.Acquire(dock, DockState.WaitAssigned);

			// Move to the waiting dock and wait for service dock to be released.
			client.QueueActivity(client.Trait<Mobile>().MoveTo(dock.Location, 2));
			client.QueueActivity(new CallFunc(() => OnArrival(client, dock)));
			client.QueueActivity(new WaitFor(() => dockClient.DockState == DockState.ServiceAssigned));
		}

		void removeDead(List<Actor> queue)
		{
			// dock release, acquire is done by DockClient trait. But, queue must be updated by DockManager.
			// It won't be too hard though.
			var rms = queue.Where(a => a.IsDead || a.IsIdle || a.Disposed).ToList();
			foreach (var rm in rms)
				queue.Remove(rm);
		}

		// Get the queue going by popping queue.
		// Then, find a suitable dock place for the client and return it.
		void processQueue(Actor self, Actor client)
		{
			checkObstacle(self);

			removeDead(queue);

			// Now serve the 1st in line, until all service docks are occupied.
			while (queue.Count > 0)
			{
				var serviceDock = serviceDocks.FirstOrDefault(d => d.Occupier == null && !d.IsBlocked);
				// find the first available slot in the service docks.
				if (serviceDock == null)
					break;
				var head = nearestClient(self, serviceDock, queue);
				serveHead(self, head, serviceDock);
				queue.Remove(head); // remove head
			}

			// was just a queue notification when the someone released the dock.
			if (client == null)
				return;

			// Is served already?
			if (!queue.Contains(client))
				return;

			serveNewClient(client);
		}

		void ITick.Tick(Actor self)
		{
			if (!info.DetectDetectionEnabled)
				return;

			if (ticks-- <= 0)
			{
				removeDeadLock(queue);
				ticks = info.DeadlockDetectionPeriod;
			}
		}
	}
}
