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

		public void OnArrival(Actor harv, Dock dock)
		{
			// We arrived at the docking spot.
			// Currently nothing to do...?
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
		// To simplify the computation, give firstPriorityDock to compute distance from.
		Actor nearestClient(Actor self, Dock firstPriorityDock, IEnumerable<Actor> queue)
		{
			Actor r = null;
			int bestDist = -1;
			foreach (var a in queue)
			{
				var vec = a.Location - firstPriorityDock.Location;
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
			client.QueueActivity(new WaitFor(() => dockClient.DockState == DockState.ServiceAssigned));
		}

		void removeDead(List<Actor> queue)
		{
			// dock release, acquire is done by DockClient trait. But, queue must be updated by DockManager.
			// It won't be too hard though.
			var rms = queue.Where(a => a.IsDead || a.IsIdle || a.Disposed);
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
			Actor head = null;
			while (queue.Count > 0)
			{
				head = nearestClient(self, serviceDocks[0], queue);
				// find the first available slot in the service docks.
				var serviceDock = serviceDocks.FirstOrDefault(d => d.Occupier == null && !d.IsBlocked);
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

			serveNewClient(client);
		}
	}
}
