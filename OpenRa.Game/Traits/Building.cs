using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class Building : ITick
	{
		public readonly BuildingInfo unitInfo;

		public Building(Actor self)
		{
			unitInfo = (BuildingInfo)self.Info;
		}

		bool first = true;
		public void Tick(Actor self)
		{
			if (first)
				self.CenterLocation = Game.CellSize * (float2)self.Location + 0.5f * self.SelectedSize;

			first = false;
		}
	}
}
