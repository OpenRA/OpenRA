#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Unit got to face the target")]
	public class AttackFrontalInfo : AttackBaseInfo
	{
		public readonly int FacingTolerance = 1;

		public override object Create(ActorInitializer init) { return new AttackFrontal(init.self, this); }
	}

	public class AttackFrontal : AttackBase
	{
		readonly AttackFrontalInfo info;

		public AttackFrontal(Actor self, AttackFrontalInfo info)
			: base(self)
		{
			this.info = info;
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (!base.CanAttack(self, target))
				return false;

			var facing = self.Trait<IFacing>().Facing;
			var facingToTarget = Util.GetFacing(target.CenterPosition - self.CenterPosition, facing);

			if (Math.Abs(facingToTarget - facing) % 256 > info.FacingTolerance)
				return false;

			return true;
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			var a = ChooseArmamentForTarget(newTarget);
			if (a == null)
				return null;

			return new Activities.Attack(newTarget, a.Weapon.Range, allowMove);
		}
	}
}
