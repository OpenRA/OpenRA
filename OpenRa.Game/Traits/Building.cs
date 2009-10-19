using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class Building : ITick
	{
		public Building(Actor self)
		{
		}

		bool first = true;
		public void Tick(Actor self, Game game)
		{
			if (first && self.Owner == game.LocalPlayer)
			{
				self.Owner.TechTree.Build(self.unitInfo.Name, true);
				self.CenterLocation = Game.CellSize * (float2)self.Location + 0.5f * self.SelectedSize;
			}
			first = false;
		}
	}
}
