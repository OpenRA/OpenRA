using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
{
	class PlaceBuilding : IOrderGenerator
	{
		public readonly Player Owner;
		public readonly UnitInfo.BuildingInfo Building;

		public PlaceBuilding(Player owner, string name)
		{
			Owner = owner;
			Building = (UnitInfo.BuildingInfo)Rules.UnitInfo[ name ];
		}

		public IEnumerable<Order> Order(int2 xy, bool lmb)
		{
			if( lmb )
			{
				if( !Game.CanPlaceBuilding( Building, xy, true ) )
					yield break;

				var maxDistance = Building.Adjacent + 2;	/* real-ra is weird. this is 1 GAP. */
				if( !Footprint.Tiles( Building, xy ).Any(
					t => Game.GetDistanceToBase( t, Owner ) < maxDistance ) )
					yield break;

				yield return OpenRa.Game.Order.PlaceBuilding( Owner, xy, Building.Name );
			}
			else // rmb
			{
				Game.world.AddFrameEndTask( _ => { Game.controller.orderGenerator = null; } );
			}
		}

		public void Tick()
		{
			var producing = Owner.Producing( Rules.UnitCategory[ Building.Name ] );
			if( producing == null || producing.Item != Building.Name || producing.RemainingTime != 0 )
				Game.world.AddFrameEndTask( _ => { Game.controller.orderGenerator = null; } );
		}

		public void Render()
		{
			Game.worldRenderer.uiOverlay.DrawBuildingGrid( Building );
		}
	}
}
