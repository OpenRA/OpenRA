#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
	[Desc("How much the unit is worth.")]
	public class ValuedInfo : TraitInfo<Valued>
	{
		[FieldLoader.Require]
		[Desc("Used in production, but also for bounties so remember to set it > 0 even for NPCs.")]
		public readonly int Cost = 0;

		[Desc("Actor's cost changes with time. Requires `VariedCostManager` on `World` actor.")]
		public readonly bool Varies = false;

		[Desc("Lower value for varied cost.")]
		public readonly int MinimumVariationMultiplier = 75;

		[Desc("Upper value for varied cost.")]
		public readonly int MaximumVariationMultiplier = 100;
	}

	public class Valued { }
}
