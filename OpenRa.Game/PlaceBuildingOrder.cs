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

		public PlaceBuildingOrder( PlaceBuilding building, int2 xy )
		{
			this.building = building;
			this.xy = xy;
		}

		public override void Apply( bool leftMouseButton )
		{
			if( leftMouseButton )
			{
				Game.world.AddFrameEndTask( _ =>
				{
					Log.Write( "Player \"{0}\" builds {1}", building.Owner.PlayerName, building.Name );

					//Adjust placement for cursor to be in middle
					Game.world.Add( new Actor( building.Name, xy - GameRules.Footprint.AdjustForBuildingSize( building.Name ), building.Owner ) );

					Game.controller.orderGenerator = null;
					Game.worldRenderer.uiOverlay.KillOverlay();
				} );
			}
			else
			{
				Game.world.AddFrameEndTask( _ =>
				{
					Game.controller.orderGenerator = null;
					Game.worldRenderer.uiOverlay.KillOverlay();
				} );
			}
		}
	}
}
