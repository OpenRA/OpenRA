using OpenRa.Game.GameRules;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class McvDeploy : IIssueOrder, IResolveOrder
	{
		public McvDeploy(Actor self) { }

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Right && self == underCursor)
				return new Order("DeployMcv", self, null, int2.Zero, null);

			return null;
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if( order.OrderString == "DeployMcv" )
			{
				var factBuildingInfo = (BuildingInfo)Rules.UnitInfo[ "fact" ];
				if( Game.CanPlaceBuilding( factBuildingInfo, self.Location - new int2( 1, 1 ), self, false ) )
				{
					self.CancelActivity();
					self.QueueActivity( new Turn( 96 ) );
					self.QueueActivity( new DeployMcv() );
				}
			}
		}
	}
}
