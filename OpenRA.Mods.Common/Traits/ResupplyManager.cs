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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be sent to a structure for repairs.")]
	public class ResupplyManagerInfo : ITraitInfo, Requires<IMoveInfo>
	{
		[VoiceReference] public readonly string Voice = "Action";

		public readonly bool RequiresEnteringResupplier = true;

		public readonly WDist CloseEnough = new WDist(512);

		public virtual object Create(ActorInitializer init) { return new ResupplyManager(init.Self, this); }
	}

	public class ResupplyManager : IIssueOrder, IResolveOrder, IOrderVoice, INotifyCreated
	{
		readonly ResupplyManagerInfo info;
		readonly IMove movement;
		Health health;
		AmmoPool[] ammoPools;
		Repairable[] repairables;
		Rearmable[] rearmables;

		public ResupplyManager(Actor self, ResupplyManagerInfo info)
		{
			this.info = info;
			movement = self.Trait<IMove>();
		}

		void INotifyCreated.Created(Actor self)
		{
			health = self.TraitOrDefault<Health>();
			ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
			repairables = self.TraitsImplementing<Repairable>().ToArray();
			rearmables = self.TraitsImplementing<Rearmable>().ToArray();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<BuildingInfo>("Repair", 5, CanRepairAt, _ => CanRepair() || CanRearm());
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Repair")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool CanRepairAt(Actor target)
		{
			return repairables.Any(r => !r.IsTraitDisabled && r.Info.RepairActors.Contains(target.Info.Name));
		}

		bool CanRearmAt(Actor target)
		{
			return rearmables.Any(r => !r.IsTraitDisabled && r.Info.RearmActors.Contains(target.Info.Name));
		}

		bool CanRepair()
		{
			return health != null && health.DamageState > DamageState.Undamaged;
		}

		bool CanRearm()
		{
			return ammoPools.Any(x => !x.AutoReloads && !x.FullAmmo());
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Repair" && CanRepair()) ? info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Repair")
			{
				if (!CanRepairAt(order.TargetActor) || (!CanRepair() && !CanRearm()))
					return;

				var target = Target.FromOrder(self.World, order);
				self.SetTargetLine(target, Color.Green);

				self.CancelActivity();
				self.QueueActivity(new WaitForTransport(self, ActivityUtils.SequenceActivities(new MoveAdjacentTo(self, target),
					new CallFunc(() => AfterReachActivities(self, order, movement)))));

				TryCallTransport(self, target, new CallFunc(() => AfterReachActivities(self, order, movement)));
			}
		}

		void AfterReachActivities(Actor self, Order order, IMove movement)
		{
			if (!order.TargetActor.IsInWorld || order.TargetActor.IsDead || order.TargetActor.TraitsImplementing<RepairsUnits>().All(r => r.IsTraitDisabled))
				return;

			// TODO: This is hacky, but almost every single component affected
			// will need to be rewritten anyway, so this is OK for now.
			if (info.RequiresEnteringResupplier)
				self.QueueActivity(movement.MoveTo(self.World.Map.CellContaining(order.TargetActor.CenterPosition), order.TargetActor));
			else
				self.QueueActivity(movement.MoveWithinRange(order.Target, info.CloseEnough));

			if (CanRearmAt(order.TargetActor) && CanRearm())
				self.QueueActivity(new Rearm(self));

			// Add a CloseEnough range to ensure we're close enough to the host actor
			self.QueueActivity(new Repair(self, order.TargetActor, info.CloseEnough));

			// If actor moved to resupplier center, try to leave it
			var rp = order.TargetActor.TraitOrDefault<RallyPoint>();
			if (rp != null && info.RequiresEnteringResupplier)
			{
				self.QueueActivity(new CallFunc(() =>
				{
					self.SetTargetLine(Target.FromCell(self.World, rp.Location), Color.Green);
					self.QueueActivity(movement.MoveTo(rp.Location, order.TargetActor));
				}));
			}
		}

		public Actor FindRepairActor(Actor self)
		{
			var repairActor = self.World.ActorsWithTrait<RepairsUnits>()
				.Where(a => !a.Actor.IsDead && a.Actor.IsInWorld
					&& a.Actor.Owner.IsAlliedWith(self.Owner)
					&& repairables.Any(r => !r.IsTraitDisabled && r.Info.RepairActors.Contains(a.Actor.Info.Name)))
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			// Worst case FirstOrDefault() will return a TraitPair<null, null>, which is OK.
			return repairActor.FirstOrDefault().Actor;
		}

		static void TryCallTransport(Actor self, Target target, Activity nextActivity)
		{
			var transport = self.TraitOrDefault<ICallForTransport>();
			if (transport == null)
				return;

			var targetCell = self.World.Map.CellContaining(target.CenterPosition);
			if ((self.CenterPosition - target.CenterPosition).LengthSquared < transport.MinimumDistance.LengthSquared)
				return;

			transport.RequestTransport(self, targetCell, nextActivity);
		}
	}
}
