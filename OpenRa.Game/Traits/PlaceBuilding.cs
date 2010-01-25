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
				self.World.AddFrameEndTask( _ =>
				{
					var queue = self.traits.Get<ProductionQueue>();
					var unit = Rules.Info[ order.TargetString ];
					var producing = queue.CurrentItem(unit.Category);
					if( producing == null || producing.Item != order.TargetString || producing.RemainingTime != 0 )
						return;

					self.World.CreateActor( order.TargetString, order.TargetLocation, order.Player );
					Sound.PlayToPlayer(order.Player, "placbldg.aud");
					Sound.PlayToPlayer(order.Player, "build5.aud");

					queue.FinishProduction(unit.Category);
				} );
			}
		}
	}
}
