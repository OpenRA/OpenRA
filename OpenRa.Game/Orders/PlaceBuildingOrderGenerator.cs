using System.Collections.Generic;
using OpenRa.GameRules;
using OpenRa.Traits;

namespace OpenRa.Orders
{
	class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		readonly Actor Producer;
		readonly string Building;
		BuildingInfo BuildingInfo { get { return Rules.Info[ Building ].Traits.Get<BuildingInfo>(); } }

		public PlaceBuildingOrderGenerator(Actor producer, string name)
		{
			Producer = producer;
			Building = name;
		}

		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				Game.controller.CancelInputMode();

			return InnerOrder(world, xy, mi);
		}

		IEnumerable<Order> InnerOrder(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var topLeft = xy - Footprint.AdjustForBuildingSize( BuildingInfo );
				if (!world.CanPlaceBuilding( Building, BuildingInfo, topLeft, null)
					|| !world.IsCloseEnoughToBase(Producer.Owner, Building, BuildingInfo, topLeft))
				{
					Sound.Play("nodeply1.aud");
					yield break;
				}

				yield return new Order("PlaceBuilding", Producer.Owner.PlayerActor, topLeft, Building);
			}
		}

		public void Tick( World world )
		{
			var producing = Producer.traits.Get<Traits.ProductionQueue>().CurrentItem( Rules.Info[ Building ].Category );
			if (producing == null || producing.Item != Building || producing.RemainingTime != 0)
				Game.controller.CancelInputMode();
		}

		public void Render( World world )
		{
			world.WorldRenderer.uiOverlay.DrawBuildingGrid( world, Building, BuildingInfo );
		}

		public Cursor GetCursor(World world, int2 xy, MouseInput mi)
		{
			return Cursor.Default;
		}
	}
}
