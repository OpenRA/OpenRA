using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class Building : ITick, INotifyBuildComplete
	{
		public readonly BuildingInfo unitInfo;

		public Building(Actor self)
		{
			unitInfo = (BuildingInfo)self.unitInfo;
		}

		bool first = true;
		public void Tick(Actor self)
		{
			if (first)
				self.CenterLocation = Game.CellSize * (float2)self.Location + 0.5f * self.SelectedSize;

			first = false;
		}

		public void BuildingComplete(Actor self)
		{
			if (self.Owner != null)
				self.Owner.ChangePower(unitInfo.Power);
		}
	}
}
