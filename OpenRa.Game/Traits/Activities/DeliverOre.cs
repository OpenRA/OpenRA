using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class DeliverOre : Activity
	{
		public Activity NextActivity { get; set; }

		bool isDone;
		Actor refinery;

		public DeliverOre( Actor refinery )
		{
			this.refinery = refinery;
		}

		static readonly int2 refineryDeliverOffset = new int2( 1, 2 );

		public void Tick(Actor self, Mobile mobile)
		{
			if( self.Location != refinery.Location + refineryDeliverOffset )
			{
				var move = new Move( refinery.Location + refineryDeliverOffset, 0 );
				mobile.InternalSetActivity( move );
				mobile.QueueActivity( this );
				move.Tick( self, mobile );
				return;
			}
			else if( mobile.facing != 64 )
			{
				var turn = new Turn( 64 );
				mobile.InternalSetActivity( turn );
				mobile.QueueActivity( this );
				turn.Tick( self, mobile );
				return;
			}
			else if (isDone)
			{
				var harv = self.traits.Get<Harvester>();

				harv.Deliver(self);

				if( NextActivity == null )
					NextActivity = new Harvest();
				mobile.InternalSetActivity(NextActivity);
				return;
			}

			var renderUnit = self.traits.WithInterface<RenderUnit>().First();
			if (renderUnit.anim.CurrentSequence.Name != "empty")
				renderUnit.PlayCustomAnimation(self, "empty", 
					() => isDone = true);
		}

		public void Cancel(Actor self, Mobile mobile)
		{
			mobile.InternalSetActivity(null);
		}
	}
}
