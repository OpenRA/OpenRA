#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Linq;

namespace OpenRA.Traits
{
	class OreGrowthInfo : ITraitInfo
	{
		public readonly float Interval = 1f;
		public readonly float Chance = .02f;
		public readonly bool Spreads = true;
		public readonly bool Grows = true;

		public object Create(Actor self) { return new OreGrowth(); }
	}

	class OreGrowth : ITick
	{
		int remainingTicks;

		public void Tick(Actor self)
		{
			if (--remainingTicks <= 0)
			{
				var info = self.Info.Traits.Get<OreGrowthInfo>();

				// HACK HACK: we should push "grows" down to the resource.
				var oreResource = self.World.WorldActor.Info.Traits.WithInterface<ResourceTypeInfo>()
					.FirstOrDefault(r => r.Name == "Ore");

				if (oreResource != null)
				{
					if (info.Spreads)
						Ore.SpreadOre(self.World,
							self.World.SharedRandom,
							info.Chance);

					if (info.Grows)
						self.World.WorldActor.traits.Get<ResourceLayer>().Grow(oreResource);
				}

				self.World.Minimap.InvalidateOre();
				remainingTicks = (int)(info.Interval * 60 * 25);
			}
		}
	}
}
