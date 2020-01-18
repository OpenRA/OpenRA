#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the production time of this actor for a specific queue or when a prerequisite is granted.")]
	public class ProductionTimeMultiplierInfo : TraitInfo<ProductionTimeMultiplier>, IProductionTimeModifierInfo
	{
		[Desc("Percentage modifier to apply.")]
		public readonly int Multiplier = 100;

		[Desc("Only apply this time change if owner has these prerequisites.")]
		public readonly string[] Prerequisites = { };

		[Desc("Queues that this time will apply.")]
		public readonly HashSet<string> Queue = new HashSet<string>();

		int IProductionTimeModifierInfo.GetProductionTimeModifier(TechTree techTree, string queue)
		{
			if ((!Queue.Any() || Queue.Contains(queue)) && (!Prerequisites.Any() || techTree.HasPrerequisites(Prerequisites)))
				return Multiplier;

			return 100;
		}
	}

	public class ProductionTimeMultiplier { }
}
