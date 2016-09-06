#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can be carried by units with the trait `Carryall`.")]
	public class CarryableInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[Desc("Required distance away from destination before requesting a pickup. Default is 6 cells.")]
		public readonly WDist MinDistance = WDist.FromCells(6);

		[UpgradeGrantedReference]
		[Desc("The upgrades to grant to self while waiting or being carried.")]
		public readonly string[] CarryableUpgrades = { };

		public object Create(ActorInitializer init) { return new Carryable(init.Self, this); }
	}

	public class Carryable : INotifyHarvesterAction, ICallForTransport
	{
		readonly CarryableInfo info;
		readonly Actor self;
		readonly UpgradeManager upgradeManager;

		public bool Reserved { get; private set; }

		// If we're locked there isn't much we can do. We'll have to wait for the carrier to finish with us. We should not move or get new orders!
		bool locked;

		void Unlock()
		{
			if (!locked)
				return;

			locked = false;
			foreach (var u in info.CarryableUpgrades)
				upgradeManager.RevokeUpgrade(self, u, this);
		}

		void Lock()
		{
			if (locked)
				return;

			locked = true;
			foreach (var u in info.CarryableUpgrades)
				upgradeManager.GrantUpgrade(self, u, this);
		}

		public bool WantsTransport { get; set; }
		public CPos Destination;
		Activity afterLandActivity;

		public Carryable(Actor self, CarryableInfo info)
		{
			this.info = info;
			this.self = self;
			upgradeManager = self.Trait<UpgradeManager>();

			locked = false;
			Reserved = false;
			WantsTransport = false;
		}

		public void MovingToResources(Actor self, CPos targetCell, Activity next) { RequestTransport(targetCell, next); }
		public void MovingToRefinery(Actor self, CPos targetCell, Activity next) { RequestTransport(targetCell, next); }

		public WDist MinimumDistance { get { return info.MinDistance; } }

		public void RequestTransport(CPos destination, Activity afterLandActivity)
		{
			var destPos = self.World.Map.CenterOfCell(destination);
			if (destination == CPos.Zero || (self.CenterPosition - destPos).LengthSquared < info.MinDistance.LengthSquared)
			{
				WantsTransport = false; // Be sure to cancel any pending transports
				return;
			}

			Destination = destination;
			this.afterLandActivity = afterLandActivity;
			WantsTransport = true;

			if (locked || Reserved)
				return;

			// Inform all idle carriers
			var carriers = self.World.ActorsWithTrait<Carryall>()
				.Where(c => !c.Trait.IsBusy && !c.Actor.IsDead && c.Actor.Owner == self.Owner && c.Actor.IsInWorld)
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			// Is any carrier able to transport the actor?
			// Any will return once it finds a carrier that returns true.
			carriers.Any(carrier => carrier.Trait.RequestTransportNotify(self));
		}

		// No longer want to be carried
		public void MovementCancelled(Actor self)
		{
			if (locked)
				return;

			WantsTransport = false;
			afterLandActivity = null;

			// TODO: We could implement something like a carrier.Trait<Carryall>().CancelTransportNotify(self) and call it here
		}

		// We do not handle Harvested notification
		public void Harvested(Actor self, ResourceType resource) { }
		public void Docked() { }
		public void Undocked() { }

		public Actor GetClosestIdleCarrier()
		{
			// Find carriers
			var carriers = self.World.ActorsHavingTrait<Carryall>(c => !c.IsBusy)
				.Where(a => a.Owner == self.Owner && a.IsInWorld);

			return carriers.ClosestTo(self);
		}

		// This gets called by carrier after we touched down
		public void Dropped()
		{
			WantsTransport = false;
			Unlock();

			if (afterLandActivity != null)
			{
				// HACK: Harvesters need special treatment to avoid getting stuck on resource fields,
				// so if a Harvester's afterLandActivity is not DeliverResources, queue a new FindResources activity
				var findResources = self.Info.HasTraitInfo<HarvesterInfo>() && !(afterLandActivity is DeliverResources);
				if (findResources)
					self.QueueActivity(new FindResources(self));
				else
					self.QueueActivity(false, afterLandActivity);
			}
		}

		public bool Reserve(Actor carrier)
		{
			if (Reserved)
				return false;

			var destPos = self.World.Map.CenterOfCell(Destination);
			if ((self.CenterPosition - destPos).LengthSquared < info.MinDistance.LengthSquared)
			{
				MovementCancelled(self);
				return false;
			}

			Reserved = true;

			return true;
		}

		public void UnReserve(Actor carrier)
		{
			Reserved = false;
			Unlock();
		}

		// Prepare for transport pickup
		public bool StandbyForPickup(Actor carrier)
		{
			if (Destination == CPos.Zero)
				return false;

			if (locked || !WantsTransport)
				return false;

			// Last chance to change our mind...
			var destPos = self.World.Map.CenterOfCell(Destination);
			if ((self.CenterPosition - destPos).LengthSquared < info.MinDistance.LengthSquared)
			{
				MovementCancelled(self);
				return false;
			}

			// Cancel our activities
			self.CancelActivity();
			Lock();

			return true;
		}
	}
}
