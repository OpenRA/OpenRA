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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be sent to a structure for repairs.")]
	public class RepairableInfo : TraitInfo, Requires<IHealthInfo>, Requires<IMoveInfo>, IObservesVariablesInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		public readonly HashSet<string> RepairActors = new HashSet<string> { };

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("The amount the unit will be repaired at each step. Use -1 for fallback behavior where HpPerStep from RepairsUnits trait will be used.")]
		public readonly int HpPerStep = -1;

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) enter cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;

		[CursorReference]
		[Desc("Cursor to display when able to be repaired at target actor.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when unable to be repaired at target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		public override object Create(ActorInitializer init) { return new Repairable(init.Self, this); }
	}

	public class Repairable : IIssueOrder, IResolveOrder, IOrderVoice, INotifyCreated, IObservesVariables
	{
		public readonly RepairableInfo Info;
		readonly IHealth health;
		Rearmable rearmable;
		bool requireForceMove;
		bool isAircraft;

		public Repairable(Actor self, RepairableInfo info)
		{
			Info = info;
			health = self.Trait<IHealth>();
		}

		void INotifyCreated.Created(Actor self)
		{
			rearmable = self.TraitOrDefault<Rearmable>();
			isAircraft = self.Info.HasTraitInfo<AircraftInfo>();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!isAircraft)
					yield return new EnterAlliedActorTargeter<BuildingInfo>(
						"Repair",
						5,
						Info.EnterCursor,
						Info.EnterBlockedCursor,
						CanRepairAt,
						_ => CanRepair() || CanRearm());
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Repair")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool CanRepairAt(Actor target)
		{
			return Info.RepairActors.Contains(target.Info.Name);
		}

		bool CanRepairAt(Actor target, TargetModifiers modifiers)
		{
			if (requireForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			return Info.RepairActors.Contains(target.Info.Name);
		}

		bool CanRepair()
		{
			return health.DamageState > DamageState.Undamaged;
		}

		bool CanRearm()
		{
			return rearmable != null && rearmable.RearmableAmmoPools.Any(p => !p.HasFullAmmo);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Repair" && (CanRepair() || CanRearm()) ? Info.Voice : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "Repair")
				return;

			// Repair orders are only valid for own/allied actors,
			// which are guaranteed to never be frozen.
			if (order.Target.Type != TargetType.Actor)
				return;

			// Aircraft handle Repair orders directly in the Aircraft trait
			// TODO: Move the order handling of both this trait and Aircraft to a generalistic DockManager
			if (isAircraft)
				return;

			if (!CanRepairAt(order.Target.Actor) || (!CanRepair() && !CanRearm()))
				return;

			self.QueueActivity(order.Queued, new Resupply(self, order.Target.Actor, new WDist(512)));
			self.ShowTargetLines();
		}

		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			if (Info.RequireForceMoveCondition != null)
				yield return new VariableObserver(RequireForceMoveConditionChanged, Info.RequireForceMoveCondition.Variables);
		}

		void RequireForceMoveConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			requireForceMove = Info.RequireForceMoveCondition.Evaluate(conditions);
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
	}
}
