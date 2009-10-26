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

		public Order Order(Actor self, int2 xy, bool lmb)
		{
			if( lmb ) return null;

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

		public override void Apply()
		{
			var mobile = Unit.traits.Get<Mobile>();
			mobile.QueueActivity( new Mobile.Turn( 96 ) );
			mobile.QueueActivity( new DeployAction() );
		}

		class DeployAction : Mobile.CurrentActivity
		{
			public Mobile.CurrentActivity NextActivity { get; set; }

			public void Tick( Actor self, Mobile mobile )
			{
				Game.world.AddFrameEndTask( _ =>
				{
					Game.world.Remove( self );
					Game.world.Add( new Actor( "fact", self.Location - new int2( 1, 1 ), self.Owner ) );
				} );
			}

			public void Cancel( Actor self, Mobile mobile )
			{
				// Cancel can't happen between this being moved to the head of the list, and it being Ticked.
				throw new InvalidOperationException( "DeployMcvAction: Cancel() should never occur." );
			}
		}
	}
}
