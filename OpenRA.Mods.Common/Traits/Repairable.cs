#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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

		[Desc("The amount the unit will be repaired at each step. Use -1 for fallback behavior where HpPerStep from RepairUnit trait will be used.")]
		public readonly int HpPerStep = -1;

		public virtual object Create(ActorInitializer init) { return new Repairable(init.Self, this); }
	}

	class Repairable : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly RepairableInfo Info;
		readonly Health health;
		readonly IMove movement;
		readonly AmmoPool[] ammoPools;

		public Repairable(Actor self, RepairableInfo info)
		{
			Info = info;
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
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool CanRepairAt(Actor target)
		{
			return Info.RepairBuildings.Contains(target.Info.Name);
		}

		bool CanRearmAt(Actor target)
		{
			return Info.RepairBuildings.Contains(target.Info.Name);
		}

		bool CanRepair()
		{
			return health.DamageState > DamageState.Undamaged;
		}

		bool CanRearm()
		{
			return ammoPools.Any(x => !x.AutoReloads && !x.FullAmmo());
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Repair" && CanRepair()) ? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Repair")
			{
				// Repair orders are only valid for own/allied actors,
				// which are guaranteed to never be frozen.
				if (order.Target.Type != TargetType.Actor)
					return;

				if (!CanRepairAt(order.Target.Actor) || (!CanRepair() && !CanRearm()))
					return;

				if (!order.Queued)
					self.CancelActivity();

				self.SetTargetLine(order.Target, Color.Green);
				self.QueueActivity(new WaitForTransport(self, ActivityUtils.SequenceActivities(new MoveAdjacentTo(self, order.Target),
					new CallFunc(() => AfterReachActivities(self, order, movement)))));

				TryCallTransport(self, order.Target, new CallFunc(() => AfterReachActivities(self, order, movement)));
			}
		}

		void AfterReachActivities(Actor self, Order order, IMove movement)
		{
			if (order.Target.Type != TargetType.Actor)
				return;

			var targetActor = order.Target.Actor;
			if (!targetActor.IsInWorld || targetActor.IsDead || targetActor.TraitsImplementing<RepairsUnits>().All(r => r.IsTraitDisabled))
				return;

			// TODO: This is hacky, but almost every single component affected
			// will need to be rewritten anyway, so this is OK for now.
			self.QueueActivity(movement.MoveTo(self.World.Map.CellContaining(targetActor.CenterPosition), targetActor));
			if (CanRearmAt(targetActor) && CanRearm())
				self.QueueActivity(new Rearm(self));

			// Add a CloseEnough range of 512 to ensure we're at the host actor
			self.QueueActivity(new Repair(self, targetActor, new WDist(512)));

			var rp = targetActor.TraitOrDefault<RallyPoint>();
			if (rp != null)
			{
				self.QueueActivity(new CallFunc(() =>
				{
					self.SetTargetLine(Target.FromCell(self.World, rp.Location), Color.Green);
					self.QueueActivity(movement.MoveTo(rp.Location, targetActor));
				}));
			}
		}

		public Actor FindRepairBuilding(Actor self)
		{
			var repairBuilding = self.World.ActorsWithTrait<RepairsUnits>()
				.Where(a => !a.Actor.IsDead && a.Actor.IsInWorld
					&& a.Actor.Owner.IsAlliedWith(self.Owner) &&
					Info.RepairBuildings.Contains(a.Actor.Info.Name))
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			// Worst case FirstOrDefault() will return a TraitPair<null, null>, which is OK.
			return repairBuilding.FirstOrDefault().Actor;
		}

		static void TryCallTransport(Actor self, Target target, Activity nextActivity)
		{
			var targetCell = self.World.Map.CellContaining(target.CenterPosition);
			var delta = (self.CenterPosition - target.CenterPosition).LengthSquared;
			var transports = self.TraitsImplementing<ICallForTransport>()
				.Where(t => t.MinimumDistance.LengthSquared < delta);

			foreach (var t in transports)
				t.RequestTransport(self, targetCell, nextActivity);
		}
	}
}
