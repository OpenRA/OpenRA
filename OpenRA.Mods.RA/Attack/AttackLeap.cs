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
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackLeapInfo : AttackFrontalInfo
	{
		public override object Create(ActorInitializer init) { return new AttackLeap(init.self, this); }
	}

	class AttackLeap : AttackFrontal, ISync
	{
		[Sync] internal bool IsLeaping;

		public AttackLeap(Actor self, AttackLeapInfo info)
			: base(self, info) {}

		public override void DoAttack(Actor self, Target target)
		{
			if( !CanAttack( self, target ) ) return;

			var weapon = Weapons[0].Info;
			if( !Combat.IsInRange( self.CenterLocation, weapon.Range, target ) ) return;

			self.CancelActivity();
			self.QueueActivity(new Leap(self, target));
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			var weapon = ChooseWeaponForTarget(newTarget);
			if( weapon == null )
				return null;
			return new Activities.Attack(newTarget, Math.Max(0, (int)weapon.Info.Range), allowMove);
		}
	}
}
