using System.Collections.Generic;
using OpenRa.GameRules;
using OpenRa.Traits;

namespace OpenRa.Orders
{
	class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		readonly Actor Producer;
		readonly string Building;
		BuildingInfo BuildingInfo { get { return Rules.NewUnitInfo[ Building ].Traits.Get<BuildingInfo>(); } }

		public PlaceBuildingOrderGenerator(Actor producer, string name)
		{
			Producer = producer;
			Building = name;
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
				if (!Game.CanPlaceBuilding( Building, BuildingInfo, xy, null, true)
					|| !Game.IsCloseEnoughToBase(Producer.Owner, Building, BuildingInfo, xy))
				{
					Sound.Play("nodeply1.aud");
					yield break;
				}

				yield return new Order("PlaceBuilding", Producer.Owner.PlayerActor, null, xy, Building);
			}
		}

		public void Tick()
		{
			var producing = Producer.traits.Get<Traits.ProductionQueue>().CurrentItem( Rules.NewUnitInfo[ Building ].Category );
			if (producing == null || producing.Item != Building || producing.RemainingTime != 0)
				Game.controller.CancelInputMode();
		}

		public void Render()
		{
			Game.worldRenderer.uiOverlay.DrawBuildingGrid( Building, BuildingInfo );
		}

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			return Cursor.Default;
		}
	}
}
