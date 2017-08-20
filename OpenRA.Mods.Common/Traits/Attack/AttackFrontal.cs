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

using System;
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Unit got to face the target")]
	public class AttackFrontalInfo : AttackBaseInfo, Requires<IFacingInfo>
	{
		public readonly int FacingTolerance = 0;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (FacingTolerance < 0 || FacingTolerance > 128)
				throw new YamlException("Facing tolerance must be in range of [0, 128], 128 covers 360 degrees.");
		}

		public override object Create(ActorInitializer init) { return new AttackFrontal(init.Self, this); }
	}

	public class AttackFrontal : AttackBase
	{
		readonly AttackFrontalInfo info;

		public AttackFrontal(Actor self, AttackFrontalInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (!base.CanAttack(self, target))
				return false;

			var pos = self.CenterPosition;
			var targetedPosition = GetTargetPosition(pos, target);
			var delta = targetedPosition - pos;

			if (delta.HorizontalLengthSquared == 0)
				return true;

			return Util.FacingWithinTolerance(facing.Facing, delta.Yaw.Facing, info.FacingTolerance);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new Activities.Attack(self, newTarget, allowMove, forceAttack, info.FacingTolerance);
		}
	}
}
