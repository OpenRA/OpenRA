using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class McvDeploy : IOrder, ITick
	{
		public int2? DeployLocation;

		public McvDeploy(Actor self)
		{
		}

		public Order Order(Actor self, int2 xy)
		{
			DeployLocation = null;
			// TODO: check that there's enough space at the destination.
			if( xy == self.Location )
				return new DeployMcvOrder( self, xy );

			return null;
		}

		public void Tick(Actor self)
		{
			if( self.Location != DeployLocation )
				return;

			var mobile = self.traits.Get<Mobile>();
			mobile.desiredFacing = 96;
			if( mobile.moveFraction < mobile.moveFractionTotal )
				return;

			if( mobile.facing != mobile.desiredFacing )
				return;

			Game.world.AddFrameEndTask(_ =>
			{
					Game.world.Remove(self);
					Game.world.Add(new Actor("fact", self.Location - new int2(1, 1), self.Owner));
			});
		}
	}
}
