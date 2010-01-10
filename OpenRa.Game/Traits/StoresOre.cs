using System.Collections.Generic;
using System;
using OpenRa.Game.GameRules;
namespace OpenRa.Game.Traits
{
	class StoresOreInfo : StatelessTraitInfo<StoresOre>
	{
		public readonly int Pips = 0;
		public readonly int Capacity = 0;
	}

	class StoresOre : IPips, IAcceptThief
	{
		public void OnSteal(Actor self, Actor thief)
		{
			// Steal half the ore the building holds
			var toSteal = self.Info.Traits.Get<StoresOreInfo>().Capacity / 2;
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
			var numPips = self.Info.Traits.Get<StoresOreInfo>().Pips;

			return Graphics.Util.MakeArray( numPips, 
				i => (Game.LocalPlayer.GetSiloFullness() > i * 1.0f / numPips) 
					? PipType.Yellow : PipType.Transparent );

		}
	}
}
