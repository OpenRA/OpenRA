#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
		}

		// A carryable notifying us that he'd like to be carried
		public override bool RequestTransportNotify(Actor self, Actor carryable, CPos destination)
		{
			if (busy)
				return false;

			if (ReserveCarryable(self, carryable))
			{
				self.QueueActivity(false, new FerryUnit(self, carryable));
				return true;
			}

			return false;
		}

		static bool IsBestAutoCarryallForCargo(Actor self, Actor candidateCargo)
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
					self.QueueActivity(false, new FerryUnit(self, p.Actor));
					break;
				}
			}
		}

		sealed class FerryUnit : Activity
		{
			readonly Actor cargo;
			readonly Carryable carryable;
			readonly Carryall carryall;

			public FerryUnit(Actor self, Actor cargo)
			{
				this.cargo = cargo;
				carryable = cargo.Trait<Carryable>();
				carryall = self.Trait<Carryall>();
			}

			protected override void OnFirstRun(Actor self)
			{
				if (!cargo.IsDead)
					QueueChild(new PickupUnit(self, cargo, 0, carryall.Info.TargetLineColor));
			}

			public override bool Tick(Actor self)
			{
				// Cargo may have become invalid or PickupUnit cancelled.
				if (carryall.Carryable == null || carryall.Carryable.IsDead)
					return true;

				var dropRange = carryall.Info.DropRange;
				self.QueueActivity(true, new DeliverUnit(self, Target.FromCell(self.World, carryable.Destination ?? self.Location), dropRange, carryall.Info.TargetLineColor));
				return true;
			}
		}
	}
}
