using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class SelfHealingInfo : ITraitInfo
	{
		public readonly int Step = 5;
		public readonly int Ticks = 5;
		public readonly float HealIfBelow = .5f;

		public object Create(Actor self) { return new SelfHealing(); }
	}

	class SelfHealing : ITick
	{
		int ticks;


		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<SelfHealingInfo>();

			if ((float)self.Health / self.GetMaxHP() >= info.HealIfBelow)
				return;

			if (--ticks <= 0)
			{
				ticks = info.Ticks;
				self.InflictDamage(self, -info.Step, Rules.WarheadInfo["Super"]);
			}
		}
	}
}
