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

using OpenRA.Activities;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Move onto the target then execute the attack.")]
	public class AttackLeapInfo : AttackFrontalInfo, Requires<MobileInfo>
	{
		[Desc("Leap speed (in WDist units/tick).")]
		public readonly WDist Speed = new WDist(426);

		[Desc("Conditions that last from start of the leap until the attack.")]
		[GrantedConditionReference]
		public readonly string LeapCondition = "attacking";

		public override object Create(ActorInitializer init) { return new AttackLeap(init.Self, this); }
	}

	public class AttackLeap : AttackFrontal
	{
		readonly AttackLeapInfo info;

		ConditionManager conditionManager;
		int leapToken = ConditionManager.InvalidConditionToken;

		public AttackLeap(Actor self, AttackLeapInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			base.Created(self);
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (target.Type != TargetType.Actor)
				return false;

			if (self.Location == target.Actor.Location && HasAnyValidWeapons(target))
				return true;

			return base.CanAttack(self, target);
		}

		public void GrantLeapCondition(Actor self)
		{
			if (conditionManager != null && !string.IsNullOrEmpty(info.LeapCondition))
				leapToken = conditionManager.GrantCondition(self, info.LeapCondition);
		}

		public void RevokeLeapCondition(Actor self)
		{
			if (leapToken != ConditionManager.InvalidConditionToken)
				leapToken = conditionManager.RevokeCondition(self, leapToken);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new LeapAttack(self, newTarget, allowMove, this, info);
		}
	}
}
