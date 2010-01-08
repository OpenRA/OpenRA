using System.Collections.Generic;
using System;
using OpenRa.Game.GameRules;
namespace OpenRa.Game.Traits
{
	class StoresOre : IPips, IAcceptThief
	{
		public const int MaxStealAmount = 100; //todo: How is cash stolen determined?
		
		readonly Actor self;
		
		public StoresOre(Actor self)
		{
			this.self = self;
		}
		
		public void OnSteal(Actor self, Actor thief)
		{
			// Steal half the ore the building holds
			var toSteal = (self.Info as BuildingInfo).Storage/2;
			self.Owner.TakeCash(toSteal);
			thief.Owner.GiveCash(toSteal);
			
			if (Game.LocalPlayer == thief.Owner)
				Sound.Play("credit1.aud");
			
			// the thief is sacrificed.
			thief.Health = 0;
			Game.world.AddFrameEndTask(w => w.Remove(thief));
		}
		
		public IEnumerable<PipType> GetPips(Actor self)
		{
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
