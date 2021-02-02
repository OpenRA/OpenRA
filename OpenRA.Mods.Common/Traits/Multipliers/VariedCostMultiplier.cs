#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the production cost of this actor for a specific queue or when a prerequisite is granted. " +
		"Requires `VariedCostManager` on the world actor.")]
	public class VariedCostMultiplierInfo : TraitInfo<VariedCostMultiplier>, IProductionCostModifierInfo, IRulesetLoaded
	{
		[Desc("Only apply this cost change if the owner has these prerequisites.")]
		public readonly string[] Prerequisites = { };

		[Desc("Production queues that this cost will apply to.")]
		public readonly HashSet<string> Queues = new HashSet<string>();

		[Desc("Set this if items should get the same random pricing.")]
		public readonly string Group = null;

		int IProductionCostModifierInfo.GetProductionCostModifier(World world, ActorInfo actorInfo, TechTree techTree, string queue)
		{
			if ((Queues.Count == 0 || Queues.Contains(queue)) && (Prerequisites.Length == 0 || techTree.HasPrerequisites(Prerequisites)))
				return world.WorldActor.Trait<VariedCostManager>().CachedCostPercentage[Group ?? actorInfo.Name];

			return 100;
		}

		public void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			var variedCostManagerInfo = rules.Actors["world"].TraitInfoOrDefault<VariedCostManagerInfo>();
			if (variedCostManagerInfo == null)
				throw new YamlException("`{0}` requires the `World` actor to have the `VariedCostManager` trait.".F(nameof(VariedCostMultiplier)));
		}
	}

	public class VariedCostMultiplier { }
}
