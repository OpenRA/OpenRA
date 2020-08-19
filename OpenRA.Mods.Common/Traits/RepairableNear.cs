#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class RepairableNearInfo : TraitInfo, Requires<IHealthInfo>, Requires<IMoveInfo>, IObservesVariablesInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		public readonly HashSet<string> RepairActors = new HashSet<string> { };

		public readonly WDist CloseEnough = WDist.FromCells(4);

		[VoiceReference]
		public readonly string Voice = "Action";

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) enter cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;

		[Desc("Cursor to display when able to be repaired near target actor.")]
		public readonly string EnterCursor = "enter";

		[Desc("Cursor to display when unable to be repaired near target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		public override object Create(ActorInitializer init) { return new RepairableNear(init.Self, this); }
	}

	public class RepairableNear : IIssueOrder, IResolveOrder, IOrderVoice, IObservesVariables
	{
		public readonly RepairableNearInfo Info;
		readonly Actor self;
		bool requireForceMove;

		public RepairableNear(Actor self, RepairableNearInfo info)
		{
			this.self = self;
			Info = info;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<BuildingInfo>(
					"RepairNear",
					5,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					CanRepairAt,
					_ => ShouldRepair());
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "RepairNear")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool CanRepairAt(Actor target, TargetModifiers modifiers)
		{
			if (requireForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			return Info.RepairActors.Contains(target.Info.Name);
		}

		bool CanRepairAt(Actor target)
		{
			return Info.RepairActors.Contains(target.Info.Name);
		}

		bool ShouldRepair()
		{
			return self.GetDamageState() > DamageState.Undamaged;
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "RepairNear" && ShouldRepair() ? Info.Voice : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			// RepairNear orders are only valid for own/allied actors,
			// which are guaranteed to never be frozen.
			if (order.OrderString != "RepairNear" || order.Target.Type != TargetType.Actor)
				return;

			if (!CanRepairAt(order.Target.Actor) || !ShouldRepair())
				return;

			self.QueueActivity(order.Queued, new Resupply(self, order.Target.Actor, Info.CloseEnough));
			self.ShowTargetLines();
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

		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			if (Info.RequireForceMoveCondition != null)
				yield return new VariableObserver(RequireForceMoveConditionChanged, Info.RequireForceMoveCondition.Variables);
		}

		void RequireForceMoveConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			requireForceMove = Info.RequireForceMoveCondition.Evaluate(conditions);
		}
	}
}
