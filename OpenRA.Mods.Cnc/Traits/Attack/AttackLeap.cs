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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Move onto the target then execute the attack.")]
	public class AttackLeapInfo : AttackBaseInfo, Requires<MobileInfo>
	{
		[Desc("Leap speed (in WDist units/tick).")]
		public readonly WDist Speed = new WDist(426);

		[Desc("Conditions that last from start of the leap until the attack.")]
		[GrantedConditionReference]
		public readonly string LeapCondition = "attacking";

		public override object Create(ActorInitializer init) { return new AttackLeap(init.Self, this); }
	}

	public class AttackLeap : AttackBase
	{
		public new readonly AttackLeapInfo Info;

		ConditionManager conditionManager;
		int leapToken = ConditionManager.InvalidConditionToken;

		public AttackLeap(Actor self, AttackLeapInfo info)
			: base(self, info)
		{
			Info = info;
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

			if (!base.CanAttack(self, target))
				return false;

			return TargetInFiringArc(self, target, Info.FacingTolerance);
		}

		public void GrantLeapCondition(Actor self)
		{
			if (conditionManager != null && !string.IsNullOrEmpty(Info.LeapCondition))
				leapToken = conditionManager.GrantCondition(self, Info.LeapCondition);
		}

		public void RevokeLeapCondition(Actor self)
		{
			if (leapToken != ConditionManager.InvalidConditionToken)
				leapToken = conditionManager.RevokeCondition(self, leapToken);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor)
		{
			return new LeapAttack(self, newTarget, allowMove, forceAttack, this, Info, targetLineColor);
		}
	}
}
