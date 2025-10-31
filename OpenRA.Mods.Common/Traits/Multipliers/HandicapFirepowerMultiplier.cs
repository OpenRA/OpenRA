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
	[Desc("Modifies the damage applied by this actor based on the owner's handicap.")]
	public class HandicapFirepowerMultiplierInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new HandicapFirepowerMultiplier(init.Self); }
	}

	public class HandicapFirepowerMultiplier : IFirepowerModifier
	{
		readonly Actor self;

		public HandicapFirepowerMultiplier(Actor self)
		{
			this.self = self;
		}

		int IFirepowerModifier.GetFirepowerModifier()
		{
			// Equivalent to the firepower handicap from C&C3:
			//  5% handicap = 95% firepower
			// 50% handicap = 50% firepower
			// 95% handicap = 5% firepower
			return 100 - self.Owner.Handicap;
		}
	}
}
