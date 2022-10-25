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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can be carried by units with the trait `" + nameof(Carryall) + "`.")]
	public class AutoCarryableInfo : CarryableInfo
	{
		[Desc("Required distance away from destination before requesting a pickup. Default is 6 cells.")]
		public readonly WDist MinDistance = WDist.FromCells(6);

		public override object Create(ActorInitializer init) { return new AutoCarryable(this); }
	}

	public class AutoCarryable : Carryable, ICallForTransport
	{
		readonly AutoCarryableInfo info;
		bool autoReserved = false;

		public CPos? Destination { get; private set; }
		public bool WantsTransport => Destination != null && !IsTraitDisabled;

		public AutoCarryable(AutoCarryableInfo info)
			: base(info)
		{
			this.info = info;
		}

		public WDist MinimumDistance => info.MinDistance;

		// No longer want to be carried
		void ICallForTransport.MovementCancelled(Actor self) { MovementCancelled(); }
		void ICallForTransport.RequestTransport(Actor self, CPos destination) { RequestTransport(self, destination); }

		void MovementCancelled()
		{
			if (state == State.Locked)
				return;

			Destination = null;
			autoReserved = false;

			// TODO: We could implement something like a carrier.Trait<Carryall>().CancelTransportNotify(self) and call it here
		}

		void RequestTransport(Actor self, CPos destination)
		{
			if (!IsValidAutoCarryDistance(self, destination))
			{
				Destination = null;
				return;
			}

			Destination = destination;

			if (state != State.Free)
				return;

			// Inform all idle carriers
			var carriers = self.World.ActorsWithTrait<AutoCarryall>()
				.Where(c => c.Trait.State == Carryall.CarryallState.Idle && !c.Trait.IsTraitDisabled && c.Trait.EnableAutoCarry && !c.Actor.IsDead && c.Actor.Owner == self.Owner && c.Actor.IsInWorld)
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			// Enumerate idle carriers to find the first that is able to transport us
			foreach (var carrier in carriers)
				if (carrier.Trait.RequestTransportNotify(carrier.Actor, self))
					return;
		}

		// This gets called by carrier after we touched down
		public override void Detached(Actor self)
		{
			if (!attached)
				return;

			Destination = null;

			base.Detached(self);
		}

		public bool AutoReserve(Actor self, Actor carrier)
		{
			if (Reserved || !WantsTransport)
				return false;

			if (!IsValidAutoCarryDistance(self, Destination.Value))
			{
				// Cancel pickup
				MovementCancelled();
				return false;
			}

			if (Reserve(self, carrier))
			{
				autoReserved = true;
				return true;
			}

			return false;
		}

		// Prepare for transport pickup
		public override LockResponse LockForPickup(Actor self, Actor carrier)
		{
			if (state == State.Locked && Carrier != carrier)
				return LockResponse.Failed;

			// When "autoReserved" is true, the carrying operation is given by auto command
			// we still need to check the validity of "Destination" to ensure an effective trip.
			if (autoReserved)
			{
				if (!WantsTransport)
				{
					// Cancel pickup
					MovementCancelled();
					return LockResponse.Failed;
				}

				if (!IsValidAutoCarryDistance(self, Destination.Value))
				{
					// Cancel pickup
					MovementCancelled();
					return LockResponse.Failed;
				}

				// Reset "autoReserved" as we finished the check
				autoReserved = false;
			}

			return base.LockForPickup(self, carrier);
		}

		bool IsValidAutoCarryDistance(Actor self, CPos destination)
		{
			if (Mobile == null)
				return false;

			// TODO: change the check here to pathfinding distance in the future
			return (self.World.Map.CenterOfCell(destination) - self.CenterPosition).HorizontalLengthSquared >= info.MinDistance.LengthSquared
				|| !Mobile.PathFinder.PathExistsForLocomotor(Mobile.Locomotor, self.Location, destination);
		}
	}
}
