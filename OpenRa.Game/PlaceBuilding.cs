using System.Collections.Generic;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
{
	class PlaceBuilding : IOrderGenerator
	{
		public readonly Player Owner;
		public readonly BuildingInfo Building;

		public PlaceBuilding(Player owner, string name)
		{
			Owner = owner;
			Building = (BuildingInfo)Rules.UnitInfo[ name ];
		}

		public IEnumerable<Order> Order(int2 xy, bool lmb)
		{
			if( lmb )
			{
				if( !Game.CanPlaceBuilding( Building, xy, null, true ) )
					yield break;

				if (!Game.IsCloseEnoughToBase(Owner, Building, xy))
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
			var producing = Owner.PlayerActor.traits.Get<Traits.ProductionQueue>().Producing( Rules.UnitCategory[ Building.Name ] );
			if( producing == null || producing.Item != Building.Name || producing.RemainingTime != 0 )
				Game.world.AddFrameEndTask( _ => { Game.controller.orderGenerator = null; } );
		}

		public void Render()
		{
			Game.worldRenderer.uiOverlay.DrawBuildingGrid( Building );
		}
	}
}
