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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Makes the Varied Cost logic work. Attach this to `World` actor.")]
	public class VariedCostManagerInfo : ITraitInfo
	{
		[Desc("How often the varied cost is recalculated.",
			"Two values indicate a random interval range.")]
		public readonly int[] VariationRecalculationInterval = { 0 };

		public object Create(ActorInitializer init) { return new VariedCostManager(init.Self, this); }
	}

	public class VariedCostManager : ITick, ISync
	{
		readonly World world;
		readonly VariedCostManagerInfo info;
		readonly ICollection<ActorInfo> units;
		public Dictionary<string, int> VariedCost = new Dictionary<string, int>();
		[Sync] int ticks;

		public VariedCostManager(Actor self, VariedCostManagerInfo info)
		{
			this.info = info;
			world = self.World;
			units = world.Map.Rules.Actors.Values;
			ticks = Util.RandomDelay(world, info.VariationRecalculationInterval);

			foreach (var unit in units)
			{
				var valued = unit.TraitInfoOrDefault<ValuedInfo>();

				if (valued != null && valued.Varies)
				{
					VariedCost.Add(unit.Name,
						world.SharedRandom.Next((valued.Cost * valued.MinimumVariationMultiplier) / 100, (valued.Cost * valued.MaximumVariationMultiplier) / 100));
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			if (--ticks < 0)
			{
				ticks = Util.RandomDelay(world, info.VariationRecalculationInterval);

				foreach (var unit in units)
				{
					var valued = unit.TraitInfoOrDefault<ValuedInfo>();
					if (valued != null && valued.Varies)
					{
						VariedCost.Remove(unit.Name);
						VariedCost.Add(unit.Name,
							world.SharedRandom.Next((valued.Cost * valued.MinimumVariationMultiplier) / 100, (valued.Cost * valued.MaximumVariationMultiplier) / 100));
					}
				}
			}
		}
	}
}
