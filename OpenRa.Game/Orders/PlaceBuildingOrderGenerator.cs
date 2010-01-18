using System.Collections.Generic;
using OpenRa.GameRules;
using OpenRa.Traits;

namespace OpenRa.Orders
{
	class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		readonly Actor Producer;
		readonly string Building;
		BuildingInfo BuildingInfo { get { return Rules.ActorInfo[ Building ].Traits.Get<BuildingInfo>(); } }

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
				var topLeft = xy - Footprint.AdjustForBuildingSize( BuildingInfo );
				if (!Game.world.CanPlaceBuilding( Building, BuildingInfo, topLeft, null)
					|| !Game.world.IsCloseEnoughToBase(Producer.Owner, Building, BuildingInfo, topLeft))
				{
					Sound.Play("nodeply1.aud");
					yield break;
				}

				yield return new Order("PlaceBuilding", Producer.Owner.PlayerActor, null, xy, Building);
			}
		}

		public void Tick()
		{
			var producing = Producer.traits.Get<Traits.ProductionQueue>().CurrentItem( Rules.ActorInfo[ Building ].Category );
			if (producing == null || producing.Item != Building || producing.RemainingTime != 0)
				Game.controller.CancelInputMode();
		}

		public void Render()
		{
			Game.world.WorldRenderer.uiOverlay.DrawBuildingGrid( Building, BuildingInfo );
		}

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			return Cursor.Default;
		}
	}
}
