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

using OpenRA.Activities;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
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

		int leapToken = Actor.InvalidConditionToken;

		public AttackLeap(Actor self, AttackLeapInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override bool CanAttack(Actor self, in Target target)
		{
			if (target.Type != TargetType.Actor)
				return false;

			if (self.Location == target.Actor.Location && HasAnyValidWeapons(target))
				return true;

			return base.CanAttack(self, target);
		}

		public void GrantLeapCondition(Actor self)
		{
			leapToken = self.GrantCondition(info.LeapCondition);
		}

		public void RevokeLeapCondition(Actor self)
		{
			if (leapToken != Actor.InvalidConditionToken)
				leapToken = self.RevokeCondition(leapToken);
		}

		public override Activity GetAttackActivity(Actor self, AttackSource source, in Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor)
		{
			return new LeapAttack(self, newTarget, allowMove, forceAttack, this, info, targetLineColor);
		}
	}
}
