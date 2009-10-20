using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game
{
	class PlaceBuildingOrder : Order
	{
		PlaceBuilding building;
		int2 xy;

		public PlaceBuildingOrder(PlaceBuilding building, int2 xy)
		{
			this.building = building;
			this.xy = xy;
		}

		public override void Apply(bool leftMouseButton)
		{
			if (leftMouseButton)
			{
				Game.world.AddFrameEndTask(_ =>
				{
					Log.Write("Player \"{0}\" builds {1}", building.Owner.PlayerName, building.Name);

					//Adjust placement for cursor to be in middle
					var footprint = Rules.Footprint.GetFootprint(building.Name);
					int maxWidth = 0;
					foreach (var row in footprint)
						if (row.Length > maxWidth)
							maxWidth = row.Length;

					Game.world.Add(new Actor(building.Name, 
						xy - new int2(maxWidth / 2, footprint.Length / 2), building.Owner));

					Game.controller.orderGenerator = null;
					Game.worldRenderer.uiOverlay.KillOverlay();
				});
			}
			else
			{
				Game.world.AddFrameEndTask(_ =>
				{
					Game.controller.orderGenerator = null;
					Game.worldRenderer.uiOverlay.KillOverlay();
				});
			}
		}
	}
}
