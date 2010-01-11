using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.Support;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Traits
{
	class GeneratesGap
	{
		Actor self;
		public GeneratesGap(Actor self)
		{
			this.self = self;
		}
		
		public IEnumerable<int2>GetShroudedTiles()
		{
			// Gap Generator building; powered down
			if (self.traits.Contains<Building>() && self.traits.Get<Building>().Disabled)
				yield break;
			
			// It won't let me return Game.FindTilesInCircle directly...?
			foreach (var t in Game.FindTilesInCircle(self.Location, Rules.General.GapRadius))
				yield return t;
		}
	}
}
