using System.Collections.Generic;

namespace OpenRa.Game.Traits
{
	class StoresOre : IPips
	{
		readonly Actor self;
		
		public StoresOre(Actor self)
		{
			this.self = self;
		}
		
		public IEnumerable<PipType> GetPips()
		{
			if (self.Info.OrePips == 0) yield break;

			for (int i = 0; i < self.Info.OrePips; i++)
			{
				if (Game.LocalPlayer.GetSiloFullness() > i * 1.0f / self.Info.OrePips)
				{
					yield return PipType.Yellow;
					continue;
				}
				yield return PipType.Transparent;
			}
		}
	}
}
