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
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Automatically transports harvesters with the Carryable trait between resource fields and refineries.")]
	public class AutoCarryallInfo : CarryallInfo
	{
		public override object Create(ActorInitializer init) { return new AutoCarryall(init.Self, this); }
	}

	public class AutoCarryall : Carryall, INotifyBecomingIdle
	{
		bool busy;

		public AutoCarryall(Actor self, AutoCarryallInfo info)
			: base(self, info) { }

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			busy = false;
			FindCarryableForTransport(self);

			// TODO: This should be handled by the aircraft trait
			if (!busy)
				self.QueueActivity(new HeliFlyCircle(self));
		}

		// A carryable notifying us that he'd like to be carried
		public override bool RequestTransportNotify(Actor self, Actor carryable, CPos destination)
		{
			if (busy)
				return false;

			if (ReserveCarryable(self, carryable))
			{
				self.QueueActivity(false, new PickupUnit(self, carryable, 0));
				self.QueueActivity(true, new DeliverUnit(self, destination));
				return true;
			}

			return false;
		}

		bool IsBestAutoCarryallForCargo(Actor self, Actor candidateCargo)
		{
			// Find carriers
			var carriers = self.World.ActorsHavingTrait<AutoCarryall>(c => !c.busy)
				.Where(a => a.Owner == self.Owner && a.IsInWorld);

			return carriers.ClosestTo(candidateCargo) == self;
		}

		void FindCarryableForTransport(Actor self)
		{
			if (!self.IsInWorld)
				return;

			// Get all carryables who want transport
			var carryables = self.World.ActorsWithTrait<Carryable>().Where(p =>
			{
				var actor = p.Actor;
				if (actor == null)
					return false;

				if (actor.Owner != self.Owner)
					return false;

				if (actor.IsDead)
					return false;

				var trait = p.Trait;
				if (trait.Reserved)
					return false;

				if (!trait.WantsTransport)
					return false;

				if (actor.IsIdle)
					return false;

				return true;
			}).OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			foreach (var p in carryables)
			{
				// Check if its actually me who's the best candidate
				if (IsBestAutoCarryallForCargo(self, p.Actor) && ReserveCarryable(self, p.Actor))
				{
					busy = true;
					self.QueueActivity(false, new PickupUnit(self, p.Actor, 0));
					self.QueueActivity(true, new DeliverUnit(self, p.Trait.Destination.Value));
					break;
				}
			}
		}
	}
}
