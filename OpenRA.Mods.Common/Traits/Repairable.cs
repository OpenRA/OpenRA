#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be sent to a structure for repairs.")]
	public class RepairableInfo : ITraitInfo, Requires<IHealthInfo>, Requires<IMoveInfo>
	{
		[FieldLoader.Require]
		[ActorReference] public readonly HashSet<string> RepairActors = new HashSet<string> { };

		[VoiceReference] public readonly string Voice = "Action";

		[Desc("The amount the unit will be repaired at each step. Use -1 for fallback behavior where HpPerStep from RepairsUnits trait will be used.")]
		public readonly int HpPerStep = -1;

		public virtual object Create(ActorInitializer init) { return new Repairable(init.Self, this); }
	}

	public class Repairable : IIssueOrder, IResolveOrder, IOrderVoice, INotifyCreated
	{
		public readonly RepairableInfo Info;
		readonly IHealth health;
		readonly IMove movement;
		Rearmable rearmable;

		public Repairable(Actor self, RepairableInfo info)
		{
			Info = info;
			health = self.Trait<IHealth>();
			movement = self.Trait<IMove>();
		}

		void INotifyCreated.Created(Actor self)
		{
			rearmable = self.TraitOrDefault<Rearmable>();
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
			return Info.RepairActors.Contains(target.Info.Name);
		}

		bool CanRearmAt(Actor target)
		{
			return rearmable != null && rearmable.Info.RearmActors.Contains(target.Info.Name);
		}

		bool CanRepair()
		{
			return health.DamageState > DamageState.Undamaged;
		}

		bool CanRearm()
		{
			return rearmable != null && rearmable.RearmableAmmoPools.Any(p => !p.FullAmmo());
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Repair" && (CanRepair() || CanRearm()) ? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Repair")
			{
				// Repair orders are only valid for own/allied actors,
				// which are guaranteed to never be frozen.
				if (order.Target.Type != TargetType.Actor)
					return;

				// Aircraft handle Repair orders directly in the Aircraft trait
				if (self.Info.HasTraitInfo<AircraftInfo>())
					return;

				if (!CanRepairAt(order.Target.Actor) || (!CanRepair() && !CanRearm()))
					return;

				if (!order.Queued)
					self.CancelActivity();

				self.SetTargetLine(order.Target, Color.Green);
				var activities = ActivityUtils.SequenceActivities(self,
					movement.MoveToTarget(self, order.Target, targetLineColor: Color.Green),
					new CallFunc(() => AfterReachActivities(self, order, movement)));

				self.QueueActivity(new WaitForTransport(self, activities));
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

			// Add a CloseEnough range of 512 to ensure we're at the host actor
			self.QueueActivity(new Resupply(self, targetActor, new WDist(512)));

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
					Info.RepairActors.Contains(a.Actor.Info.Name))
				.OrderBy(a => a.Actor.Owner == self.Owner ? 0 : 1)
				.ThenBy(p => (self.Location - p.Actor.Location).LengthSquared);

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
