#region Copyright & License Information
/*
 * Dock client module by Boolbada of OP Mod.
 *
 * OpenRA Copyright info:
 *
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
	public class DockClientInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DockClient(init, this); }
	}

	public enum DockState
	{
		NotAssigned,
		WaitAssigned,
		ServiceAssigned
	}

	// When dockmanager manages docked units, these units require dock client trait.
	public class DockClient : INotifyKilled, INotifyBecomingIdle, INotifyActorDisposing, INotifyOwnerChanged, IResolveOrder
	{
		// readonly DockClientInfo info;
		readonly Actor self;
		Actor host;
		Dock currentDock;

		public DockState DockState = DockState.NotAssigned;
		public IDockActivity Requester; // The activity that requested dock.

		int acquireTimeStamp = -1;

		public Actor Host { get { return host; } }

		public Dock CurrentDock { get { return currentDock; } }

		public DockClient(ActorInitializer init, DockClientInfo info)
		{
			// this.info = info;
			self = init.Self;
		}

		public void Acquire(Actor host, Dock dock, DockState dockState)
		{
			// You are to acquire only when you don't have one.
			// i.e., release first.
			Release();

			System.Diagnostics.Debug.Assert(currentDock == null, "To acquire dock, release first.");
			dock.Reserver = self;
			currentDock = dock;
			DockState = dockState;
			this.host = host;

			acquireTimeStamp = self.World.WorldTick;
		}

		// Release what we are currently holding
		public void Release()
		{
			if (currentDock != null)
				currentDock.Reserver = null;

			currentDock = null;
			DockState = DockState.NotAssigned;
			acquireTimeStamp = -1;
			host = null;
			//// Do NOT reset Requester! Deadlock resoluiton needs it.
		}

		public bool WaitedLong(int threshold)
		{
			if (acquireTimeStamp < 0)
				return false;
			return (self.World.WorldTick - acquireTimeStamp) >= threshold;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			Release();
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			Release();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			Release();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			Release();
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.Queued)
				return;

			switch (order.OrderString)
			{
				case "Enter":
				case "Deliver":
				case "ReturnToBase":
				case "Repair":
					// Prevent race condition.
					// i.e., other order acquires the dock then
					// this gets evaled and releases it! Not good.
					break;
				default:
					Release();
					break;
			}
		}
	}
}
