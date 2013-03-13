#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class AttackFrontalInfo : AttackBaseInfo
	{
		public readonly int FacingTolerance = 1;

		public override object Create( ActorInitializer init ) { return new AttackFrontal( init.self, this ); }
	}

	public class AttackFrontal : AttackBase
	{
		readonly AttackFrontalInfo info;
		public AttackFrontal(Actor self, AttackFrontalInfo info)
			: base( self ) { this.info = info; }

		protected override bool CanAttack( Actor self, Target target )
		{
			if( !base.CanAttack( self, target ) )
				return false;

			var facing = self.Trait<IFacing>().Facing;
			var facingToTarget = Util.GetFacing(target.CenterLocation - self.CenterLocation, facing);

			if( Math.Abs( facingToTarget - facing ) % 256 > info.FacingTolerance )
				return false;

			return true;
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			var weapon = ChooseArmamentForTarget(newTarget);
			if (weapon == null)
				return null;
			return new Activities.Attack(newTarget, Math.Max(0, (int)weapon.Weapon.Range), allowMove);
		}
	}
}
