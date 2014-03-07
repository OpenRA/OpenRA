#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class AttackLoyaltyInfo : AttackFrontalInfo
	{
		public override object Create(ActorInitializer init) { return new AttackLoyalty(init.self, this); }
	}

	public class AttackLoyalty : AttackFrontal
	{
		public AttackLoyalty(Actor self, AttackLoyaltyInfo info)
			: base(self, info) { }

		public override void DoAttack(Actor self, Target target)
		{
			if (!CanAttack(self, target)) return;

			var arm = Armaments.FirstOrDefault();
			if (arm == null)
				return;

			if (!target.IsInRange(self.CenterPosition, arm.Weapon.Range))
				return;

			var facing = self.TraitOrDefault<IFacing>();
			foreach (var a in Armaments)
				a.CheckFire(self, this, facing, target);

			if (target.Actor != null)
				target.Actor.ChangeOwner(self.Owner);
		}
	}
}
