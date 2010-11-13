#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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

		protected override bool CanAttack( Actor self )
		{
			if( !base.CanAttack( self ) )
				return false;

			var facing = self.Trait<IFacing>().Facing;
			var facingToTarget = Util.GetFacing(target.CenterLocation - self.CenterLocation, facing);

			if( Math.Abs( facingToTarget - facing ) % 256 > info.FacingTolerance )
				return false;

			return true;
		}

		protected override void QueueAttack(Actor self, bool queued, Target newTarget)
		{
			var weapon = ChooseWeaponForTarget(newTarget);

			if (weapon != null)
				self.QueueActivity( queued,
					new Activities.Attack(
						newTarget, 
						Math.Max(0, (int)weapon.Info.Range)));
		}
	}
}
