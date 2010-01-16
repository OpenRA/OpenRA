using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Support;
using OpenRa.Traits;

namespace OpenRa.Traits
{
	class GeneratesGapInfo : ITraitInfo
	{
		public readonly int Range = 10;
		public object Create(Actor self) { return new GeneratesGap(self); }
	}

	class GeneratesGap
	{
		Actor self;
		public GeneratesGap(Actor self)
		{
			this.self = self;
		}

		public IEnumerable<int2> GetShroudedTiles()
		{
			int range = self.Info.Traits.Get<GeneratesGapInfo>().Range;

			// Gap Generator building; powered down
			return (self.traits.Contains<Building>() && self.traits.Get<Building>().Disabled) 
				? new int2[] {} 
				: Game.FindTilesInCircle(self.Location, range);
		}
	}
}
