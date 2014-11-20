#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion
using System;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	[Desc("Can be carried by units with the trait `AutoCarryall`.")]
	public class CarryableInfo : ITraitInfo
	{
		[Desc("Required distance away from destination before requesting a pickup.")]
		public int MinDistance = 6;

		public object Create(ActorInitializer init) { return new Carryable(init.self, this); }
	}

	public class Carryable : IDisableMove, INotifyHarvesterAction
	{
		readonly CarryableInfo info;
		readonly Actor self;

		public bool Reserved { get; private set; }

		// If we're locked there isnt much we can do. We'll have to wait for the carrier to finish with us. We should not move or get new orders!
		bool locked;

		public bool WantsTransport { get; private set; }
		public CPos Destination;
		Activity afterLandActivity;

		public Carryable(Actor self, CarryableInfo info)
		{
			this.info = info;
			this.self = self;

			locked = false;
			Reserved = false;
			WantsTransport = false;
		}

		public void MovingToResources(Actor self, CPos targetCell, Activity next) { RequestTransport(targetCell, next); }
		public void MovingToRefinery(Actor self, CPos targetCell, Activity next) { RequestTransport(targetCell, next); }

		void RequestTransport(CPos destination, Activity afterLandActivity)
		{
			if (locked || Reserved)
				return;

			if (destination == CPos.Zero)
				return;

			if ((self.Location - destination).Length < info.MinDistance)
				return;

			Destination = destination;
			this.afterLandActivity = afterLandActivity;
			WantsTransport = true;

			// Inform all idle carriers
			var carriers = self.World.ActorsWithTrait<AutoCarryall>()
				.Where(c => !c.Trait.Busy && !c.Actor.IsDead && c.Actor.Owner == self.Owner)
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			foreach (var carrier in carriers)
			{
				// Notify the carrier and see if he's willing to transport us..
				if (carrier.Trait.RequestTransportNotify(self))
					break; // If true then we're done
			}
		}

		// No longer want to be carried
		public void MovementCancelled(Actor self)
		{
			if (locked)
				return;

			WantsTransport = false;
			Reserved = false;

			// TODO: We could implement something like a carrier.Trait<AutoCarryAll>().CancelTransportNotify(self) and call it here
		}

		// We do not handle Harvested notification
		public void Harvested(Actor self, ResourceType resource) { }

		public Actor GetClosestIdleCarrier()
		{
			// Find carriers
			var carriers = self.World.ActorsWithTrait<AutoCarryall>()
				.Where(p => p.Actor.Owner == self.Owner && !p.Trait.Busy)
				.Select(h => h.Actor);

			return WorldUtils.ClosestTo(carriers, self);
		}

		// This gets called by carrier after we touched down
		public void Dropped()
		{
			WantsTransport = false;
			locked = false;

			if (afterLandActivity != null)
				self.QueueActivity(false, afterLandActivity);
		}

		public bool Reserve(Actor carrier)
		{
			if ((self.Location - Destination).Length < info.MinDistance)
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
			locked = false;
		}

		// Prepare for transport pickup
		public bool StandbyForPickup(Actor carrier)
		{
			if (Destination == CPos.Zero)
				return false;

			if (locked || !WantsTransport)
				return false;

			// Last change to change our mind...
			if ((self.Location - Destination).Length < info.MinDistance)
			{
				MovementCancelled(self);
				return false;
			}

			// Cancel our activities
			self.CancelActivity();
			locked = true;

			return true;
		}

		// IMoveDisabled
		public bool MoveDisabled(Actor self)
		{
			// We do not want to move while being locked. The carrier will try to pick us up.
			return locked;
		}
	}
}
