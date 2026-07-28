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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Unit carries a finite oxygen supply. It drains while the unit is exposed",
		"to vacuum and refills while a pressurised condition is granted (e.g. by a",
		"dome's ProximityExternalCondition). Grants a condition when fully depleted",
		"so other traits (DamagedByVacuum, engine shutdown, UI bar) can react.")]
	public class OxygenInfo : PausableConditionalTraitInfo
	{
		[Desc("Maximum oxygen units.")]
		public readonly int Capacity = 6000;

		[Desc("Oxygen consumed per tick while exposed to vacuum.")]
		public readonly int DrainRate = 2;

		[Desc("Oxygen restored per tick while pressurised.")]
		public readonly int RefillRate = 40;

		[GrantedConditionReference]
		[Desc("Condition granted while oxygen is fully depleted.")]
		public readonly string DepletedCondition = "no-oxygen";

		[ConsumedConditionReference]
		[Desc("Boolean condition expression that is true while the unit is pressurised",
			"(inside a dome / sealed cabin). Oxygen refills instead of draining while true.")]
		public readonly BooleanExpression PressurisedCondition = null;

		public override object Create(ActorInitializer init) { return new Oxygen(this); }
	}

	public class Oxygen : PausableConditionalTrait<OxygenInfo>, ITick, ISync, IObservesVariables
	{
		[Sync]
		public int Current;

		int depletedToken = Actor.InvalidConditionToken;
		bool pressurised;

		public Oxygen(OxygenInfo info)
			: base(info)
		{
			Current = info.Capacity;
		}

		// Event-driven: the callback fires only when a referenced condition changes,
		// so we never poll the whole condition set every tick.
		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			if (Info.PressurisedCondition != null)
				yield return new VariableObserver(PressureChanged, Info.PressurisedCondition.Variables);
		}

		void PressureChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			pressurised = Info.PressurisedCondition.Evaluate(conditions);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (pressurised)
				Current = System.Math.Min(Info.Capacity, Current + Info.RefillRate);
			else
				Current = System.Math.Max(0, Current - Info.DrainRate);

			var depleted = Current <= 0;
			if (depleted && depletedToken == Actor.InvalidConditionToken)
				depletedToken = self.GrantCondition(Info.DepletedCondition);
			else if (!depleted && depletedToken != Actor.InvalidConditionToken)
				depletedToken = self.RevokeCondition(depletedToken);
		}

		// 0..1 for UI (see OxygenBar).
		public float Fraction => Info.Capacity == 0 ? 0f : (float)Current / Info.Capacity;
	}
}
