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
	public class AttackMedicInfo : AttackFrontalInfo
	{
		public override object Create( ActorInitializer init ) { return new AttackMedic( init.self, this ); }
	}

	public class AttackMedic : AttackFrontal
	{
		public AttackMedic(Actor self, AttackMedicInfo info)
			: base( self, info ) {}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			var weapon = ChooseWeaponForTarget(newTarget);
			if( weapon == null )
				return null;
			return new Activities.Heal(newTarget, Math.Max(0, (int)weapon.Info.Range), allowMove);
		}
	}
}
