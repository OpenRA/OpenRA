#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the damage applied to this actor based on the owner's handicap.")]
	public class HandicapDamageMultiplierInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new HandicapDamageMultiplier(init.Self); }
	}

	public class HandicapDamageMultiplier : IDamageModifier
	{
		readonly Actor self;

		public HandicapDamageMultiplier(Actor self)
		{
			this.self = self;
		}

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			// Equivalent to the health handicap from C&C3:
			//  5% handicap = 95% health = 105% damage
			// 50% handicap = 50% health = 200% damage
			// 95% handicap = 5% health = 2000% damage
			return 10000 / (100 - self.Owner.Handicap);
		}
	}
}
