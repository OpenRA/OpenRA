using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class SeedsOre : ITick
	{
		public SeedsOre( Actor self ) {}

		const double OreSeedProbability = .05;

		public void Tick(Actor self)
		{
			for (var j = -1; j < 2; j++)
				for (var i = -1; i < 2; i++)
					if (Game.SharedRandom.NextDouble() < OreSeedProbability)
						if (Ore.CanSpreadInto(self.Location.X + i, self.Location.Y + j))
							Rules.Map.AddOre(self.Location.X + i, self.Location.Y + j);
		}
	}
}
