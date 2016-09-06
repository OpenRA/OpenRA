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
	class RepairableInfo : ITraitInfo, Requires<HealthInfo>, Requires<IMoveInfo>
	{
		public readonly HashSet<string> RepairBuildings = new HashSet<string> { "fix" };

		[VoiceReference] public readonly string Voice = "Action";

		public virtual object Create(ActorInitializer init) { return new Repairable(init.Self, this); }
	}

	class Repairable : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly RepairableInfo info;
		readonly Health health;
		readonly IMove movement;
		readonly AmmoPool[] ammoPools;

		public Repairable(Actor self, RepairableInfo info)
		{
			this.info = info;
			health = self.Trait<Health>();
			movement = self.Trait<IMove>();
			ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
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
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		bool CanRepairAt(Actor target)
		{
			return info.RepairBuildings.Contains(target.Info.Name);
		}

		bool CanRearmAt(Actor target)
		{
			return info.RepairBuildings.Contains(target.Info.Name);
		}

		bool CanRepair()
		{
			return health.DamageState > DamageState.Undamaged;
		}

		bool CanRearm()
		{
			return ammoPools.Any(x => !x.Info.SelfReloads && !x.FullAmmo());
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
			if (!order.TargetActor.IsInWorld || order.TargetActor.IsDead || order.TargetActor.IsDisabled())
				return;

			// TODO: This is hacky, but almost every single component affected
			// will need to be rewritten anyway, so this is OK for now.
			self.QueueActivity(movement.MoveTo(self.World.Map.CellContaining(order.TargetActor.CenterPosition), order.TargetActor));
			if (CanRearmAt(order.TargetActor) && CanRearm())
				self.QueueActivity(new Rearm(self));

			self.QueueActivity(new Repair(order.TargetActor));

			var rp = order.TargetActor.TraitOrDefault<RallyPoint>();
			if (rp != null)
			{
				self.QueueActivity(new CallFunc(() =>
				{
					self.SetTargetLine(Target.FromCell(self.World, rp.Location), Color.Green);
					self.QueueActivity(movement.MoveTo(rp.Location, order.TargetActor));
				}));
			}
		}

		public Actor FindRepairBuilding(Actor self)
		{
			var repairBuilding = self.World.ActorsWithTrait<RepairsUnits>()
				.Where(a => !a.Actor.IsDead && a.Actor.IsInWorld
					&& a.Actor.Owner.IsAlliedWith(self.Owner) &&
					info.RepairBuildings.Contains(a.Actor.Info.Name))
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			// Worst case FirstOrDefault() will return a TraitPair<null, null>, which is OK.
			return repairBuilding.FirstOrDefault().Actor;
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
