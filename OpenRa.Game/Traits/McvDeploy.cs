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

		public Order Order(Actor self, int2 xy)
		{
			// TODO: check that there's enough space at the destination.
			if( xy == self.Location )
				return new DeployMcvOrder( self, xy );

			return null;
		}
	}

	class DeployMcvOrder : Order
	{
		Actor Unit;
		int2 Location;

		public DeployMcvOrder( Actor unit, int2 location )
		{
			Unit = unit;
			Location = location;
		}

		public override void Apply( bool leftMouseButton )
		{
			if( leftMouseButton ) return;
			Unit.traits.Get<Mobile>().SetNextAction( new Mobile.Turn( 96 ) { NextAction = new DeployAction() } );
		}

		class DeployAction : Mobile.CurrentAction
		{
			public Mobile.CurrentAction NextAction { get; set; }

			public void Tick( Actor self, Mobile mobile )
			{
				Game.world.AddFrameEndTask( _ =>
				{
					Game.world.Remove( self );
					Game.world.Add( new Actor( "fact", self.Location - new int2( 1, 1 ), self.Owner ) );
				} );
			}
		}
	}
}
