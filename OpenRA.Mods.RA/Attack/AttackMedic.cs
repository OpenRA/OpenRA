#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Give the unit a \"heal-weapon\" that attacks friendly targets if they are damaged.",
		"It conflicts with any other weapon or Attack*: trait because it will hurt friendlies during the",
		"heal process then. It also won't work with buildings (use RepairsUnits: for them)")]
	public class AttackMedicInfo : AttackFrontalInfo
	{
		public override object Create(ActorInitializer init) { return new AttackMedic(init.self, this); }
	}

	public class AttackMedic : AttackFrontal
	{
		public AttackMedic(Actor self, AttackMedicInfo info)
			: base(self, info) { }

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			var a = ChooseArmamentForTarget(newTarget);
			if (a == null)
				return null;

			return new Activities.Heal(self, newTarget, a.Weapon.MinRange, a.Weapon.Range, allowMove);
		}
	}
}
