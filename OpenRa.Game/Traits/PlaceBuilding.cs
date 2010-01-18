using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.GameRules;

namespace OpenRa.Traits
{
	class PlaceBuildingInfo : StatelessTraitInfo<PlaceBuilding> { }

	class PlaceBuilding : IResolveOrder
	{
		public void ResolveOrder( Actor self, Order order )
		{
			if( order.OrderString == "PlaceBuilding" )
			{
				Game.world.AddFrameEndTask( _ =>
				{
					var queue = self.traits.Get<ProductionQueue>();
					var unit = Rules.ActorInfo[ order.TargetString ];
					var producing = queue.CurrentItem(unit.Category);
					if( producing == null || producing.Item != order.TargetString || producing.RemainingTime != 0 )
						return;

					Game.world.CreateActor( order.TargetString, order.TargetLocation, order.Player );
					if (order.Player == Game.LocalPlayer)
					{
						Sound.Play("placbldg.aud");
						Sound.Play("build5.aud");
					}

					queue.FinishProduction(unit.Category);
				} );
			}
		}
	}
}
