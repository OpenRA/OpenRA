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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class DockManagerInfo : ITraitInfo, Requires<DockInfo>
	{
		[Desc("Are any of the docks lie outside the building footprint? (and needs obstacle checking)")]
		public readonly bool ExternalDocks = false;

		[Desc("Dock next to the actor like RA1 naval yard?",
			"Although dock position is ignored, one dummy dock is still required to determine DockAngle and stuff when docking.")]
		public readonly bool DockNextToActor = false;

		[Desc("Enable deadlock detection")]
		public readonly bool DeadlockDetectionEnabled = true;

		[Desc("Dead lock detection sampling is done this often.")]
		public readonly int DeadlockDetectionPeriod = 457; // prime number yay =~ 30 seconds

		public object Create(ActorInitializer init) { return new DockManager(init, this); }
	}

	/*
	 * The class to do all the crazy queue management.
	 * Not all multi-dock guys need queue management so making a separate manager class.
	 */
	public class DockManager : ITick, INotifyKilled, INotifyActorDisposing
	{
		readonly DockManagerInfo info;
		readonly Actor host; // Don't use "self" as the name. Eventually, it gets confusing with client and causes bug!

		readonly Dock[] allDocks;
		readonly Dock[] serviceDocks;
		readonly Dock[] waitDocks;
		readonly List<Actor> queue = new List<Actor>();

		CPos lastLocation; // in case this is a mobile dock.
		int ticks;

		public DockManager(ActorInitializer init, DockManagerInfo info)
		{
			host = init.Self;
			this.info = info;
			ticks = info.DeadlockDetectionPeriod;

			// sort the dock traits by their Order trait.
			var t0 = host.TraitsImplementing<Dock>().ToList();
			t0.Sort(delegate(Dock a, Dock b) { return a.Info.Order - b.Info.Order; });
			var t1 = t0.Where(d => !d.Info.WaitingPlace).ToList();
			var t2 = t0.Where(d => d.Info.WaitingPlace).ToList();
			allDocks = t0.ToArray();
			serviceDocks = t1.ToArray();
			waitDocks = t2.ToArray();
		}

		public bool HasFreeServiceDock(Actor client)
		{
			// This one is usually used by actors who will NOT use
			// waiting docks (aircrafts).
			// It makes sense to update dock status here.
			CheckObstacle(host);
			RemoveDead(queue);

			foreach (var d in serviceDocks)
			{
				if (d.Reserver == null)
					return true;
				if (d.Reserver == client)
					return true;
			}

			return false;
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
				return serviceDocks.Where(d => d.Reserver != null).Select(d => d.Reserver);
			}
		}

		void CheckObstacle(Actor host)
		{
			if (info.DockNextToActor)
				return;

			if (!info.ExternalDocks)
				return;

			if (host.Location == lastLocation)
				return;
			lastLocation = host.Location;

			foreach (var d in allDocks)
				d.CheckObstacle();
		}

		// Parameters: sometimes the activity that requests ReserveDock knows well on how to do the docking.
		// To help docking, pass this as param.
		public void ReserveDock(Actor host, Actor client, IDockActivity requester)
		{
			if (info.DockNextToActor)
			{
				ServeAdjacentDocker(host, client, requester);
				return;
			}

			// First, put the new client in the queue then process it.
			if (!queue.Contains(client))
			{
				queue.Add(client);

				// Initialize this noob.
				// It might had been transferred from proc A to this proc.
				var dc = client.Trait<DockClient>();
				dc.DockState = DockState.NotAssigned;
				dc.Requester = requester;
			}

			// notify the queue
			ProcessQueue(host, client);
		}

		// This client cancels the dock.
		void CancelDock(Actor a)
		{
			if (a == null || a.IsDead || a.Disposed)
				return;

			var dc = a.Trait<DockClient>();
			dc.Release(dc.CurrentDock);

			a.CancelActivity();
			var act = dc.Requester.ActivitiesOnDockFail(a);
			if (act != null)
				a.QueueActivity(act);
		}

		// Cancel on request or death (host's mass cancel notification to all clients)
		public void CancelDockAllClients()
		{
			foreach (var a in serviceDocks.Select(d => d.Reserver))
				CancelDock(a);

			foreach (var a in queue)
				CancelDock(a);
		}

		// OnDock is called, even when client arrives at a waiting dock.
		public void OnDock(Actor client, Dock dock)
		{
			// We have "arrived". But did we get to where we intended?
			var dc = client.Trait<DockClient>();

			if (client == null || client.IsDead || client.Disposed)
				return;

			if (host == null || host.IsDead || host.Disposed)
			{
				dc.Release(dc.CurrentDock);
				CancelDock(client);
				return;
			}

			// I tried to arrive at a waiting spot but actually it was a working dock and I'm sitting on it!
			// (happens often when only one dock which is shared)
			if (dock.Info.WaitingPlace == false && dc.DockState == DockState.WaitAssigned && client.Location == dock.Location)
			{
				dc.Release(dc.CurrentDock);
				client.CancelActivity();
				ProcessQueue(host, client);
				return;
			}

			// Properly docked.
			if (dock.Info.WaitingPlace == false && dc.DockState == DockState.ServiceAssigned && client.Location == dock.Location)
			{
				// resource transfer activities are queued by OnDock.
				client.QueueActivity(dc.Requester.DockActivities(host, client, dock));
				client.QueueActivity(new CallFunc(() => ReleaseAndNext(client, dock)));
				client.QueueActivity(dc.Requester.ActivitiesAfterDockDone(host, client, dock));
			}
		}

		void RemoveDeadLock(List<Actor> queue)
		{
			bool locked = false;
			var occupiers = new List<Actor>();

			foreach (var d in allDocks)
			{
				foreach (var a in host.World.ActorMap.GetActorsAt(d.Location))
				{
					var dc = a.TraitOrDefault<DockClient>();
					var mobile = a.TraitOrDefault<Mobile>();

					if (host.Owner.Stances[a.Owner] != Stance.Ally)
						continue;

					// Not even my client. Get off my dock.
					if (dc == null && mobile != null)
					{
						mobile.Nudge(a, host, true);
						continue;
					}
					else if (dc == null) // do nothing, probably non-reloading aircraft or something.
						continue;

					if (dc.CurrentDock != null && dc.WaitedLong(info.DeadlockDetectionPeriod))
						locked = true;
				}
			}
			
			if (locked)
				ResetDocks();
		}

		void ResetDocks()
		{
			// Expensive, but a sure solution where all locked guys get rescued.
			// We don't get deadlocks very often so, let's be sure to remove then when they occur.
			var clients = host.World.ActorsHavingTrait<DockClient>().Where(a => a.Trait<DockClient>().Host == host);
			queue.Clear();

			foreach (var a in clients)
			{
				if (a.IsDead || a.Disposed)
					continue;

				var dc = a.Trait<DockClient>();
				a.CancelActivity();
				dc.Release(dc.CurrentDock);
				ReserveDock(host, a, dc.Requester);
			}

			ProcessQueue(host, null);
		}

		public void ReleaseAndNext(Actor client, Dock dock)
		{
			client.Trait<DockClient>().Release(dock);

			if (host.IsDead || host.Disposed)
				return;
			ProcessQueue(host, null); // notify queue
		}

		void ServeAdjacentDocker(Actor host, Actor client, IDockActivity requester)
		{
			// Since there is 0 tolerance about distance, we WILL arrive at the dock (or we get stuck haha)
			client.QueueActivity(new MoveAdjacentTo(client, Target.FromActor(host)));

			var dock = host.Trait<Dock>();

			// resource transfer activities are queued by OnDock.
			client.QueueActivity(requester.DockActivities(host, client, dock));
			client.QueueActivity(requester.ActivitiesAfterDockDone(host, client, dock));
		}

		void ServeHead(Actor host, Actor head, Dock serviceDock)
		{
			var dockClient = head.Trait<DockClient>();
			var currentDock = dockClient.CurrentDock;

            /*
			cd == null means the queue is not so busy that the head is a new comer.
            (for example, head == client case)
			With thi in mind, 4 cases of null/not nullness of dock and cd:

			cd == null and dock == null    ERROR: What? Docks can't be busy when cd == null. Can't happen.
			cd == null and dock != null    Safe to serve.
			cd != null and dock == null    ERROR: First in line, has nowhere to go? Can't happen.
			cd != null and dock != null    Was in the waiting queue and now ready to serve.

			So, except for the errorneous state that can't happen, head is safe to serve.
			We rule out dock == null case in outer loop, before calling this function though.
            */

			dockClient.Release(currentDock);
			dockClient.Acquire(host, serviceDock, DockState.ServiceAssigned);

			head.QueueActivity(dockClient.Requester.ApproachDockActivities(host, head, serviceDock));
			head.QueueActivity(new CallFunc(() => OnDock(head, serviceDock)));
		}

		// As the actors are coming from all directions, first request, first served is not good.
		// Let it be first come first served.
		// We approximate distance computation by Rect-linear distance here, not Euclidean dist.
		Actor NearestClient(Actor host, Dock dock, IEnumerable<Actor> queue)
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

		void ServeNewClient(Actor client)
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
				dock = waitDocks.FirstOrDefault(d => d.Reserver == null && !d.IsBlocked);

				// on nothing, share the last slot.
				if (dock == null)
					dock = waitDocks.Last();
			}

			// For last dock, current dock and occupier will be messed up but doesn't matter.
			// The last one is shared anyway. The vacancy info is not very meaningful there.
			if (dockClient.CurrentDock != null)
				dockClient.Release(dockClient.CurrentDock);
			dockClient.Acquire(host, dock, DockState.WaitAssigned);

			// Move to the waiting dock and wait for service dock to be released.
			client.QueueActivity(client.Trait<Mobile>().MoveTo(dock.Location, 2));
			client.QueueActivity(new CallFunc(() => OnDock(client, dock)));
			client.QueueActivity(new WaitFor(() => dockClient.DockState == DockState.ServiceAssigned));
		}

		void RemoveDead(List<Actor> queue)
		{
			// dock release, acquire is done by DockClient trait. But, queue must be updated by DockManager.
			// It won't be too hard though.

			// hack: For refinaries idle ones should be excluded.
			List<Actor> rms;
			if (host.TraitOrDefault<Refinery>() != null)
				rms = queue.Where(a => a.IsDead || a.IsIdle || a.Disposed).ToList();
			else
				rms = queue.Where(a => a.IsDead || a.Disposed).ToList();

			foreach (var rm in rms)
				queue.Remove(rm);
		}

		// Get the queue going by popping queue.
		// Then, find a suitable dock place for the client and return it.
		void ProcessQueue(Actor host, Actor client)
		{
			CheckObstacle(host);
			RemoveDead(queue);

			// Now serve the 1st in line, until all service docks are occupied.
			while (queue.Count > 0)
			{
				// find the first available slot in the service docks.
				var serviceDock = serviceDocks.FirstOrDefault(d => d.Reserver == null && !d.IsBlocked);
				if (serviceDock == null)
					break;
				var head = NearestClient(host, serviceDock, queue);
				ServeHead(host, head, serviceDock);
				queue.Remove(head); // remove head
			}

			// was just a queue notification when the someone released the dock.
			if (client == null)
				return;

			// Is served already?
			if (!queue.Contains(client))
				return;

			ServeNewClient(client);
		}

		public static bool IsInQueue(Actor host, Actor client)
		{
			return host.Trait<DockManager>().queue.Contains(client);
		}

		void ITick.Tick(Actor host)
		{
			if (!info.DeadlockDetectionEnabled)
				return;

			if (ticks-- <= 0)
			{
				RemoveDeadLock(queue);
				ticks = info.DeadlockDetectionPeriod;
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			CancelDockAllClients();
		}

		public void Disposing(Actor self)
		{
			CancelDockAllClients();
		}
	}
}
