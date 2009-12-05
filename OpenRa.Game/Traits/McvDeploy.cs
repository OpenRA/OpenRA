using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class McvDeploy : IOrder
	{
		public McvDeploy(Actor self) { }

		public Order IssueOrder(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if (lmb) return null;
			if( xy != self.Location ) return null;

			return Order.DeployMcv(self);
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if( order.OrderString == "DeployMcv" )
			{
				var factBuildingInfo = (BuildingInfo)Rules.UnitInfo[ "fact" ];
				if( Game.CanPlaceBuilding( factBuildingInfo, self.Location - new int2( 1, 1 ), self, false ) )
				{
					self.CancelActivity();
					self.QueueActivity( new Traits.Activities.Turn( 96 ) );
					self.QueueActivity( new Traits.Activities.DeployMcv() );
				}
			}
		}
	}
}
