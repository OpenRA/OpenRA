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
		Activity afterLandActivity;

		public AutoCarryable(Actor self, AutoCarryableInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public WDist MinimumDistance { get { return info.MinDistance; } }

		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell, Activity next) { RequestTransport(self, targetCell, next); }
		void INotifyHarvesterAction.MovingToRefinery(Actor self, CPos targetCell, Activity next) { RequestTransport(self, targetCell, next); }
		void INotifyHarvesterAction.MovementCancelled(Actor self) { MovementCancelled(self); }

		// We do not handle Harvested notification
		void INotifyHarvesterAction.Harvested(Actor self, ResourceType resource) { }
		void INotifyHarvesterAction.Docked() { }
		void INotifyHarvesterAction.Undocked() { }

		// No longer want to be carried
		void ICallForTransport.MovementCancelled(Actor self) { MovementCancelled(self); }
		void ICallForTransport.RequestTransport(Actor self, CPos destination, Activity afterLandActivity) { RequestTransport(self, destination, afterLandActivity); }

		void MovementCancelled(Actor self)
		{
			if (state == State.Locked)
				return;

			Destination = null;
			afterLandActivity = null;

			// TODO: We could implement something like a carrier.Trait<Carryall>().CancelTransportNotify(self) and call it here
		}

		void RequestTransport(Actor self, CPos destination, Activity afterLandActivity)
		{
			var delta = self.World.Map.CenterOfCell(destination) - self.CenterPosition;
			if (delta.HorizontalLengthSquared < info.MinDistance.LengthSquared)
			{
				Destination = null;
				return;
			}

			Destination = destination;
			this.afterLandActivity = afterLandActivity;

			if (state != State.Free)
				return;

			// Inform all idle carriers
			var carriers = self.World.ActorsWithTrait<Carryall>()
				.Where(c => c.Trait.State == Carryall.CarryallState.Idle && !c.Actor.IsDead && c.Actor.Owner == self.Owner && c.Actor.IsInWorld)
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			// Enumerate idle carriers to find the first that is able to transport us
			foreach (var carrier in carriers)
				if (carrier.Trait.RequestTransportNotify(carrier.Actor, self, destination))
					return;
		}

		// This gets called by carrier after we touched down
		public override void Detached(Actor self)
		{
			if (!attached)
				return;

			Destination = null;

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

			base.Detached(self);
		}

		public override bool Reserve(Actor self, Actor carrier)
		{
			if (Reserved || !WantsTransport)
				return false;

			var delta = self.World.Map.CenterOfCell(Destination.Value) - self.CenterPosition;
			if (delta.HorizontalLengthSquared < info.MinDistance.LengthSquared)
			{
				// Cancel pickup
				MovementCancelled(self);
				return false;
			}

			return base.Reserve(self, carrier);
		}

		// Prepare for transport pickup
		public override bool LockForPickup(Actor self, Actor carrier)
		{
			if (state == State.Locked || !WantsTransport)
				return false;

			// Last chance to change our mind...
			var delta = self.World.Map.CenterOfCell(Destination.Value) - self.CenterPosition;
			if (delta.HorizontalLengthSquared < info.MinDistance.LengthSquared)
			{
				// Cancel pickup
				MovementCancelled(self);
				return false;
			}

			return base.LockForPickup(self, carrier);
		}
	}
}
