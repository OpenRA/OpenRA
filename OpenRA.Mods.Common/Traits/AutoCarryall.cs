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
		[Desc("Set to false when the carryall should not automatically get new jobs.")]
		public readonly bool Automatic = true;

		public override object Create(ActorInitializer init) { return new AutoCarryall(init.Self, this); }
	}

	public class AutoCarryall : Carryall, INotifyBecomingIdle
	{
		readonly Actor self;
		readonly AutoCarryallInfo info;

		bool busy;

		public AutoCarryall(Actor self, AutoCarryallInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;
			busy = false;
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			if (info.Automatic)
				FindCarryableForTransport();

			if (!busy)
				self.QueueActivity(new HeliFlyCircle(self));
		}

		// A carryable notifying us that he'd like to be carried
		public override bool RequestTransportNotify(Actor carryable)
		{
			if (busy || !info.Automatic)
				return false;

			if (ReserveCarryable(carryable))
			{
				self.QueueActivity(false, new PickupUnit(self, carryable, 0));
				self.QueueActivity(true, new DeliverUnit(self, 0));
				return true;
			}

			return false;
		}

		Actor GetClosestIdleCarrier()
		{
			// Find carriers
			var carriers = self.World.ActorsHavingTrait<AutoCarryall>(c => !c.busy)
				.Where(a => a.Owner == self.Owner && a.IsInWorld);

			return carriers.ClosestTo(self);
		}

		void FindCarryableForTransport()
		{
			if (!self.IsInWorld)
				return;

			// Get all carryables who want transport
			var carryables = self.World.ActorsWithTrait<Carryable>()
				.Where(p =>
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
				})
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			foreach (var p in carryables)
			{
				// Check if its actually me who's the best candidate
				if (GetClosestIdleCarrier() == self && ReserveCarryable(p.Actor))
				{
					self.QueueActivity(false, new PickupUnit(self, p.Actor, 0));
					self.QueueActivity(true, new DeliverUnit(self, 0));
					break;
				}
			}
		}
	}
}
