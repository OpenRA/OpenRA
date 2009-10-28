using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class McvDeploy : IOrder
	{
		public McvDeploy(Actor self)
		{
		}

		public Order Order(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if( lmb ) return null;

			// TODO: check that there's enough space at the destination.
			if( xy == self.Location )
				return OpenRa.Game.Order.DeployMcv( self );

			return null;
		}
	}
}
