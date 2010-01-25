using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.GameRules;

namespace OpenRa.Traits
{
	class SquishByTankInfo : ITraitInfo
	{
		public object Create(Actor self) { return new SquishByTank(self); }
	}

	class SquishByTank : ICrushable
	{
		readonly Actor self;
		public SquishByTank(Actor self)
		{
			this.self = self;
		}

		public void OnCrush(Actor crusher)
		{
			self.InflictDamage(crusher, self.Health, Rules.WarheadInfo["Crush"]);
		}
		
		public bool IsPathableCrush(UnitMovementType umt, Player player)
		{
			return IsCrushableBy(umt, player);
		}
		
		public bool IsCrushableBy(UnitMovementType umt, Player player)
		{
			if (player == self.Owner) return false;
			switch (umt)
			{
				case UnitMovementType.Track: return true;
				default: return false;
			}
		}
	}
}
