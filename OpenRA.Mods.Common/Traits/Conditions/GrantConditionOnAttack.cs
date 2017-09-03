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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Trigger grant condition during Attack trait activities.")]
	public class GrantConditionOnAttackInfo : ITraitInfo, Requires<ConditionManagerInfo>
	{
		[Desc("Conditions that last until the attack is executed.")]
		[GrantedConditionReference]
		public readonly string PreparingAttackCondition;

		[Desc("Timed conditions for the duration of the fire delay.")]
		[GrantedConditionReference]
		public readonly string AttackingCondition;

		[Desc("How long the timed attacking condition should last.")]
		public readonly int AttackingDuration = 100;

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (AttackingDuration <= 0)
				throw new YamlException("AttackingDuration must be a positive number!");
		}

		public object Create(ActorInitializer init) { return new GrantConditionOnAttack(init, this); }
	}

	public class GrantConditionOnAttack : INotifyAttack
	{
		readonly Actor self;
		readonly GrantConditionOnAttackInfo info;

		ConditionManager conditionManager;
		int preparingAttackConditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnAttack(ActorInitializer init, GrantConditionOnAttackInfo info)
		{
			self = init.Self;
			this.info = info;
			conditionManager = self.Trait<ConditionManager>();
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (!string.IsNullOrEmpty(info.PreparingAttackCondition) &&
					preparingAttackConditionToken == ConditionManager.InvalidConditionToken)
				preparingAttackConditionToken = conditionManager.GrantCondition(self, info.PreparingAttackCondition);
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (!string.IsNullOrEmpty(info.PreparingAttackCondition) &&
					preparingAttackConditionToken != ConditionManager.InvalidConditionToken)
				preparingAttackConditionToken = conditionManager.RevokeCondition(self, preparingAttackConditionToken);

			if (!string.IsNullOrEmpty(info.AttackingCondition))
				conditionManager.GrantCondition(self, info.AttackingCondition, info.AttackingDuration);
		}
	}
}
