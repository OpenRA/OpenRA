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
			if (mi.Button == MouseButton.Right)
				Game.controller.CancelInputMode();

			return InnerOrder(xy, mi);
		}

		IEnumerable<Order> InnerOrder(int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				if (!Game.CanPlaceBuilding(Building, xy, null, true)
					|| !Game.IsCloseEnoughToBase(Producer.Owner, Building, xy))
				{
					Sound.Play("nodeply1.aud");
					yield break;
				}

				yield return new Order("PlaceBuilding", Producer.Owner.PlayerActor, null, xy, Building.Name);
			}
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

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			return Cursor.Default;
		}
	}
}
