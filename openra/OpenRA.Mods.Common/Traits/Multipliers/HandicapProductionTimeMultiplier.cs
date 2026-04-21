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
	[Desc("Modifies the production time of this actor based on the producer's handicap.")]
	public class HandicapProductionTimeMultiplierInfo : TraitInfo<HandicapProductionTimeMultiplier>, IProductionTimeModifierInfo
	{
		int IProductionTimeModifierInfo.GetProductionTimeModifier(TechTree techTree, string queue)
		{
			// Equivalent to the build speed handicap from C&C3:
			//  5% handicap = 105% build time
			// 50% handicap = 150% build time
			// 95% handicap = 195% build time
			return 100 + techTree.Owner.Handicap;
		}
	}

	public class HandicapProductionTimeMultiplier { }
}
