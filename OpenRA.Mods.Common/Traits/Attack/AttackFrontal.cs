#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
		public readonly int FacingTolerance = 1;

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

			var f = facing.Value.Facing;
			var delta = target.CenterPosition - self.CenterPosition;
			var facingToTarget = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : f;

			if (Math.Abs(facingToTarget - f) % 256 > info.FacingTolerance)
				return false;

			return true;
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new Activities.Attack(self, newTarget, allowMove, forceAttack);
		}
	}
}
