using System.Collections.Generic;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Orders
{
	class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		readonly Actor Producer;
		readonly BuildingInfo Building;

		public PlaceBuildingOrderGenerator(Actor producer, string name)
		{
			Producer = producer;
			Building = (BuildingInfo)Rules.UnitInfo[ name ];
		}

		public IEnumerable<Order> Order(int2 xy, MouseInput mi)
		{
			if (mi.IsFake)
			{
				// this order is never actually issued, but it's used for choosing a cursor
				yield return new Order("PlaceBuilding", Producer.Owner.PlayerActor, null, xy, Building.Name);
				yield break;
			}

			if (mi.Button == MouseButton.Left)
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

				yield return new Order("PlaceBuilding", Producer.Owner.PlayerActor, null, xy, Building.Name);
			}
			else
				Game.controller.CancelInputMode();
		}

		public void Tick()
		{
			var producing = Producer.traits.Get<Traits.ProductionQueue>().CurrentItem( Rules.UnitCategory[ Building.Name ] );
			if (producing == null || producing.Item != Building.Name || producing.RemainingTime != 0)
				Game.controller.CancelInputMode();
		}

		public void Render()
		{
			Game.worldRenderer.uiOverlay.DrawBuildingGrid( Building );
		}
	}
}
