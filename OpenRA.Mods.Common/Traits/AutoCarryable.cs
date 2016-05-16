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
	public class AutoCarryableInfo : CarryableInfo
	{
		[Desc("Required distance away from destination before requesting a pickup. Default is 6 cells.")]
		public readonly WDist MinDistance = WDist.FromCells(6);

		public override object Create(ActorInitializer init) { return new AutoCarryable(init.Self, this); }
	}

	public class AutoCarryable : Carryable, INotifyHarvesterAction, ICallForTransport
	{
		readonly AutoCarryableInfo info;
		readonly Actor self;
		readonly long minDistanceSquared;

		Activity afterLandActivity;

		public AutoCarryable(Actor self, AutoCarryableInfo info)
			: base(self, info)
		{
			this.info = info;
			this.self = self;

			WantsTransport = false;
			minDistanceSquared = info.MinDistance.LengthSquared;
		}

		public WDist MinimumDistance { get { return info.MinDistance; } }

		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell, Activity next) { RequestTransport(targetCell, next); }
		void INotifyHarvesterAction.MovingToRefinery(Actor self, CPos targetCell, Activity next) { RequestTransport(targetCell, next); }
		void INotifyHarvesterAction.MovementCancelled(Actor self) { MovementCancelled(); }

		// We do not handle Harvested notification
		void INotifyHarvesterAction.Harvested(Actor self, ResourceType resource) { }
		void INotifyHarvesterAction.Docked() { }
		void INotifyHarvesterAction.Undocked() { }

		// No longer want to be carried
		void ICallForTransport.MovementCancelled(Actor self) { MovementCancelled(); }
		void ICallForTransport.RequestTransport(CPos destination, Activity afterLandActivity) { RequestTransport(destination, afterLandActivity); }

		void MovementCancelled()
		{
			if (state == State.Locked)
				return;

			WantsTransport = false;
			afterLandActivity = null;

			// TODO: We could implement something like a carrier.Trait<Carryall>().CancelTransportNotify(self) and call it here
		}

		long CalculateDistanceSquared(CPos destination)
		{
			return (self.CenterPosition - self.World.Map.CenterOfCell(destination)).LengthSquared;
		}

		void RequestTransport(CPos destination, Activity afterLandActivity)
		{
			if (destination == CPos.Zero || CalculateDistanceSquared(destination) < minDistanceSquared)
			{
				WantsTransport = false; // Be sure to cancel any pending transports
				return;
			}

			Destination = destination;
			this.afterLandActivity = afterLandActivity;
			WantsTransport = true;

			if (state != State.Free)
				return;

			// Inform all idle carriers
			var carriers = self.World.ActorsWithTrait<Carryall>()
				.Where(c => c.Trait.State == Carryall.CarryallState.Idle && !c.Actor.IsDead && c.Actor.Owner == self.Owner && c.Actor.IsInWorld)
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			// Is any carrier able to transport the actor?
			// Any will return once it finds a carrier that returns true.
			carriers.Any(carrier => carrier.Trait.RequestTransportNotify(self));
		}

		// This gets called by carrier after we touched down
		public override void Detached()
		{
			if (!attached)
				return;

			WantsTransport = false;

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

			base.Detached();
		}

		public override bool Reserve(Actor carrier)
		{
			if (Reserved)
				return false;

			if (CalculateDistanceSquared(Destination) < minDistanceSquared)
			{
				// cancel pickup
				MovementCancelled();
				return false;
			}

			return base.Reserve(carrier);
		}

		// Prepare for transport pickup
		public override bool LockForPickup(Actor carrier)
		{
			if (Destination == CPos.Zero)
				return false;

			if (state == State.Locked || !WantsTransport)
				return false;

			// Last chance to change our mind...
			if (CalculateDistanceSquared(Destination) < minDistanceSquared)
			{
				// cancel pickup
				MovementCancelled();
				return false;
			}

			return base.LockForPickup(carrier);
		}
	}
}
