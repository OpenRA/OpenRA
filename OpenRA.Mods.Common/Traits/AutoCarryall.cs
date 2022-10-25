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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Automatically transports harvesters with the AutoCarryable and CarryableHarvester between resource fields and refineries.")]
	public class AutoCarryallInfo : CarryallInfo
	{
		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the auto carry behavior is enabled. Enabled by default.")]
		public readonly BooleanExpression AutoCarryCondition = null;

		public override object Create(ActorInitializer init) { return new AutoCarryall(init.Self, this); }
	}

	public class AutoCarryall : Carryall, INotifyBecomingIdle, IObservesVariables
	{
		readonly AutoCarryallInfo info;

		public bool EnableAutoCarry { get; private set; }

		public AutoCarryall(Actor self, AutoCarryallInfo info)
			: base(self, info)
		{
			this.info = info;
			EnableAutoCarry = true;
		}

		static bool Busy(Actor self) => self.CurrentActivity != null && self.CurrentActivity is not FlyIdle;

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			if (!EnableAutoCarry || IsTraitDisabled)
				return;

			FindCarryableForTransport(self);
		}

		public override IEnumerable<VariableObserver> GetVariableObservers()
		{
			foreach (var observer in base.GetVariableObservers())
				yield return observer;

			if (info.AutoCarryCondition != null)
				yield return new VariableObserver(AutoCarryConditionsChanged, info.AutoCarryCondition.Variables);
		}

		void AutoCarryConditionsChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			EnableAutoCarry = info.AutoCarryCondition.Evaluate(conditions);
		}

		// A carryable notifying us that he'd like to be carried
		public bool RequestTransportNotify(Actor self, Actor carryable)
		{
			if (Busy(self) || IsTraitDisabled || !EnableAutoCarry)
				return false;

			if (AutoReserveCarryable(self, carryable))
			{
				self.QueueActivity(false, new FerryUnit(self, carryable));
				return true;
			}

			return false;
		}

		bool AutoReserveCarryable(Actor self, Actor carryable)
		{
			if (State == CarryallState.Reserved)
				UnreserveCarryable(self);

			if (State != CarryallState.Idle)
				return false;

			var act = carryable.TraitOrDefault<AutoCarryable>();

			if (act == null || !act.AutoReserve(carryable, self))
				return false;

			Carryable = carryable;
			State = CarryallState.Reserved;
			return true;
		}

		static bool IsBestAutoCarryallForCargo(Actor self, Actor candidateCargo)
		{
			// Find carriers
			var carriers = self.World.ActorsHavingTrait<AutoCarryall>(c => !Busy(self) && c.EnableAutoCarry)
				.Where(a => a.Owner == self.Owner && a.IsInWorld);

			return carriers.ClosestTo(candidateCargo) == self;
		}

		void FindCarryableForTransport(Actor self)
		{
			if (!self.IsInWorld || IsTraitDisabled)
				return;

			// Get all carryables who want transport
			var carryables = self.World.ActorsWithTrait<AutoCarryable>().Where(p =>
			{
				var actor = p.Actor;
				if (actor == null)
					return false;

				if (actor.Owner != self.Owner)
					return false;

				if (actor.IsDead)
					return false;

				var trait = p.Trait;
				if (trait.Reserved)
					return false;

				if (!trait.WantsTransport)
					return false;

				if (actor.IsIdle)
					return false;

				return true;
			}).OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			foreach (var p in carryables)
			{
				// Check if its actually me who's the best candidate
				if (IsBestAutoCarryallForCargo(self, p.Actor) && AutoReserveCarryable(self, p.Actor))
				{
					self.QueueActivity(false, new FerryUnit(self, p.Actor));
					break;
				}
			}
		}

		sealed class FerryUnit : Activity
		{
			readonly Actor cargo;
			readonly AutoCarryable carryable;
			readonly AutoCarryall carryall;

			public FerryUnit(Actor self, Actor cargo)
			{
				this.cargo = cargo;
				carryable = cargo.Trait<AutoCarryable>();
				carryall = self.Trait<AutoCarryall>();
			}

			protected override void OnFirstRun(Actor self)
			{
				if (!carryall.IsTraitDisabled && carryall.Carryable != null && !carryall.Carryable.IsDead)
					QueueChild(new PickupUnit(self, cargo, 0, carryall.Info.TargetLineColor));
			}

			public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
			{
				if (ChildActivity != null)
				{
					// Draw a line to destination if haven't pick up the cargo
					if (ChildActivity is PickupUnit)
					{
						yield return new TargetLineNode(Target.FromActor(cargo), carryall.Info.TargetLineColor);
						if (carryable.Destination != null)
							yield return new TargetLineNode(Target.FromCell(self.World, carryable.Destination.Value), carryall.Info.TargetLineColor);
					}
					else
						foreach (var n in ChildActivity.TargetLineNodes(self))
							yield return n;
				}
			}

			public override bool Tick(Actor self)
			{
				// Cargo may have become invalid or PickupUnit cancelled.
				if (IsCanceling || carryall.IsTraitDisabled || carryall.Carryable == null || carryall.Carryable.IsDead)
					return true;

				var dropRange = carryall.Info.DropRange;
				if (carryable.Destination != null)
					QueueChild(new DeliverUnit(self, Target.FromCell(self.World, carryable.Destination.Value), dropRange, carryall.Info.TargetLineColor));

				return true;
			}
		}
	}
}
