#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Unit got to face the target")]
	public class AttackFrontalInfo : AttackBaseInfo, Requires<IFacingInfo>
	{
		[Desc("Tolerance for attack angle. Range [0, 128], 128 covers 360 degrees.")]
		public readonly new int FacingTolerance = 0;

		public override object Create(ActorInitializer init) { return new AttackFrontal(init.Self, this); }
	}

	public class AttackFrontal : AttackBase
	{
		public new readonly AttackFrontalInfo Info;

		public AttackFrontal(Actor self, AttackFrontalInfo info)
			: base(self, info)
		{
			Info = info;
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (!base.CanAttack(self, target))
				return false;

			return TargetInFiringArc(self, target, Info.FacingTolerance);
		}

		public override Activity GetAttackActivity(Actor self, AttackSource source, Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor = null)
		{
			return new Activities.Attack(self, newTarget, allowMove, forceAttack, targetLineColor);
		}
	}
}
