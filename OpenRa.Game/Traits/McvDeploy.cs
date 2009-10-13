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

		public Order Order(Actor self, Game game, int2 xy)
		{
			// TODO: check that there's enough space at the destination.
			if( xy == self.Location )
				return new DeployMcvOrder( self, xy );

			return null;
		}

		public void Tick(Actor self, Game game, int dt)
		{
			var mobile = self.traits.Get<Mobile>();

			if( mobile.moveFraction < mobile.moveFractionTotal )
				return;

			if( self.Location != DeployLocation )
			{
				DeployLocation = null;
				return;
			}

			if (mobile.Turn(12))
				return;

			game.world.AddFrameEndTask(_ =>
			{
				game.world.Remove(self);
				game.world.Add(new Actor("fact", self.Location - new int2(1, 1), self.Owner));
			});
		}
	}
}
