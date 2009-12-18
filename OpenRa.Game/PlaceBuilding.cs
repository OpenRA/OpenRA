using System.Collections.Generic;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
{
	class PlaceBuilding : IOrderGenerator
	{
		readonly Actor Producer;
		readonly BuildingInfo Building;

		public PlaceBuilding(Actor producer, string name)
		{
			Producer = producer;
			Building = (BuildingInfo)Rules.UnitInfo[ name ];
		}

		public IEnumerable<Order> Order(int2 xy, MouseInput mi)
		{
			if( mi.Button == MouseButton.Left )
			{
                if (!Game.CanPlaceBuilding(Building, xy, null, true))
                {
                    Sound.Play("nodeply1.aud");
                    yield break;
                }

                if (!Game.IsCloseEnoughToBase(Producer.Owner, Building, xy))
                {
                    Sound.Play("nodeply1.aud");
                    yield break;
                }

				yield return OpenRa.Game.Order.PlaceBuilding( Producer.Owner, xy, Building.Name );
			}
			else // rmb
			{
				Game.world.AddFrameEndTask( _ => { Game.controller.orderGenerator = null; } );
			}
		}

		public void Tick()
		{
			var producing = Producer.traits.Get<Traits.ProductionQueue>().Producing( Rules.UnitCategory[ Building.Name ] );
			if( producing == null || producing.Item != Building.Name || producing.RemainingTime != 0 )
				Game.world.AddFrameEndTask( _ => { Game.controller.orderGenerator = null; } );
		}

		public void Render()
		{
			Game.worldRenderer.uiOverlay.DrawBuildingGrid( Building );
		}
	}
}
