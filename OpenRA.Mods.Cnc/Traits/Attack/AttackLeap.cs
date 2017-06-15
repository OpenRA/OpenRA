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

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Move onto the target then execute the attack. Remark: MinRange ignored. This is assumed to be a melee attacking behaviour.")]
	public class AttackLeapInfo : AttackFrontalInfo, Requires<MobileInfo>
	{
		[Desc("Leap speed (in WDist units/tick).")]
		public readonly WDist Speed = new WDist(426);

		[Desc("Conditions that last from start of leap till attack.")]
		[GrantedConditionReference]
		public readonly string LeapCondition = "leap";

		[Desc("Conditions to grant on approaching out-or-range targets")]
		[GrantedConditionReference]
		public readonly string ApproachCondition = "rush";

		public override object Create(ActorInitializer init) { return new AttackLeap(init.Self, this); }
	}

	public class AttackLeap : AttackFrontal, INotifyCreated
	{
		readonly AttackLeapInfo info;

		ConditionManager conditionManager;
		int approachToken = ConditionManager.InvalidConditionToken;
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

		// LeapAttack.Cancel() never gets called so handling that with OnStopOrder here.
		protected override void OnStopOrder(Actor self)
		{
			LeapBuffOff(self);
			ApproachBuffOff(self);
			base.OnStopOrder(self);
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			// No facing check when we reached the target
			if (target.Actor != null && self.Location == target.Actor.Location && HasAnyValidWeapons(target))
				return true;

			return base.CanAttack(self, target);
		}

		public void ApproachBuffOn(Actor self)
		{
			if (conditionManager != null && string.IsNullOrEmpty(info.ApproachCondition))
				approachToken = conditionManager.GrantCondition(self, info.ApproachCondition);
		}

		public void ApproachBuffOff(Actor self)
		{
			if (approachToken != ConditionManager.InvalidConditionToken)
				approachToken = conditionManager.RevokeCondition(self, approachToken);
		}

		public void LeapBuffOn(Actor self)
		{
			if (conditionManager != null && string.IsNullOrEmpty(info.LeapCondition))
				leapToken = conditionManager.GrantCondition(self, info.LeapCondition);
		}

		public void LeapBuffOff(Actor self)
		{
			if (leapToken != ConditionManager.InvalidConditionToken)
				leapToken = conditionManager.RevokeCondition(self, leapToken);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new LeapAttack(self, newTarget, allowMove, info);
		}
	}
}
