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

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the production time of this actor for a specific queue or when a prerequisite is granted.")]
	public class ProductionTimeMultiplierInfo : TraitInfo<ProductionTimeMultiplier>, IProductionTimeModifierInfo
	{
		[Desc("Percentage modifier to apply.")]
		public readonly int Multiplier = 100;

		[Desc("Only apply this time change if owner has these prerequisites.")]
		public readonly string[] Prerequisites = Array.Empty<string>();

		[Desc("Queues that this time will apply.")]
		public readonly HashSet<string> Queue = new HashSet<string>();

		int IProductionTimeModifierInfo.GetProductionTimeModifier(TechTree techTree, string queue)
		{
			if ((Queue.Count == 0 || Queue.Contains(queue)) && (Prerequisites.Length == 0 || techTree.HasPrerequisites(Prerequisites)))
				return Multiplier;

			return 100;
		}
	}

	public class ProductionTimeMultiplier { }
}
