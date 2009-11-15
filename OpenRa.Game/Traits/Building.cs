using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class Building : ITick, INotifyBuildComplete
	{
		public readonly UnitInfo.BuildingInfo unitInfo;

		public Building(Actor self)
		{
			unitInfo = (UnitInfo.BuildingInfo)self.unitInfo;
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
			self.Owner.ChangePower(unitInfo.Power);
		}
	}
}
