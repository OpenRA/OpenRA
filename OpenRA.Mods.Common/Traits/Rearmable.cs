#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class RearmableInfo : TraitInfo, Requires<AmmoPoolInfo>
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actors that this actor can dock to and get rearmed by.")]
		public readonly HashSet<string> RearmActors = new HashSet<string> { };

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Name(s) of AmmoPool(s) that use this trait to rearm.")]
		public readonly HashSet<string> AmmoPools = new HashSet<string> { "primary" };

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) enter cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;

		[CursorReference]
		[Desc("Cursor to display when able to be repaired at target actor.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when unable to be repaired at target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		public override object Create(ActorInitializer init) { return new Rearmable(this); }
	}

	public class Rearmable : IIssueOrder, IResolveOrder, IOrderVoice, INotifyCreated, INotifyResupply, IObservesVariables
	{
		public readonly RearmableInfo Info;

		public Rearmable(RearmableInfo info)
		{
			Info = info;
		}

		public AmmoPool[] RearmableAmmoPools { get; private set; }

		bool isAircraft;
		bool isRepairable;
		bool requireForceMove;

		void INotifyCreated.Created(Actor self)
		{
			RearmableAmmoPools = self.TraitsImplementing<AmmoPool>().Where(p => Info.AmmoPools.Contains(p.Info.Name)).ToArray();
			isAircraft = self.Info.HasTraitInfo<AircraftInfo>();
			isRepairable = self.Info.HasTraitInfo<RepairableInfo>();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!isAircraft || !isRepairable)
					yield return new EnterAlliedActorTargeter<BuildingInfo>(
						"Rearm",
						5,
						Info.EnterCursor,
						Info.EnterBlockedCursor,
						CanRearmAt,
						_ => CanRearm());
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Rearm")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		public bool CanRearmAt(Actor target)
		{
			return Info.RearmActors.Contains(target.Info.Name);
		}

		public bool CanRearmAt(Actor target, TargetModifiers modifiers)
		{
			if (requireForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			return CanRearmAt(target);
		}

		public bool CanRearm()
		{
			return RearmableAmmoPools.Any(p => !p.HasFullAmmo);
		}

		void INotifyResupply.BeforeResupply(Actor self, Actor target, ResupplyType types)
		{
			if (!types.HasFlag(ResupplyType.Rearm))
				return;

			// Reset the ReloadDelay to avoid any issues with early cancellation
			// from previous reload attempts (explicit order, host building died, etc).
			foreach (var pool in RearmableAmmoPools)
				pool.RemainingTicks = pool.Info.ReloadDelay;
		}

		void INotifyResupply.ResupplyTick(Actor self, Actor target, ResupplyType types) { }

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Rearm" && CanRearm() ? Info.Voice : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "Rearm")
				return;

			// Repair orders are only valid for own/allied actors,
			// which are guaranteed to never be frozen.
			if (order.Target.Type != TargetType.Actor)
				return;

			// Aircraft handle Repair orders directly in the Aircraft trait
			// TODO: Move the order handling of both this trait and Aircraft to a generalistic DockManager
			if (isAircraft || isRepairable)
				return;

			if (!CanRearmAt(order.Target.Actor) || !CanRearm())
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
	}
}
