using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Graphics;

namespace OpenRa.Traits
{
	class OreGrowthInfo : ITraitInfo
	{
		public readonly float Interval = 1f;
		public readonly float Chance = .02f;
		public readonly bool Spreads = true;
		public readonly bool Grows = true;

		public object Create(Actor self) { return new OreGrowth(); }
	}

	class OreGrowth : ITick, ILoadWorldHook
	{
		int remainingTicks;

		public void Tick(Actor self)
		{
			if (--remainingTicks <= 0)
			{
				var info = self.Info.Traits.Get<OreGrowthInfo>();
				
				if (info.Spreads) 
					Ore.SpreadOre(self.World, 
						self.World.SharedRandom,
						info.Chance);

				if (info.Grows)
					Ore.GrowOre(self.World, self.World.SharedRandom);

				self.World.Minimap.InvalidateOre();
				remainingTicks = (int)(info.Interval * 60 * 25);
			}
		}

		public void WorldLoaded(World w)
		{
			Ore.InitOreDensity(w.Map);
		}
	}
}
