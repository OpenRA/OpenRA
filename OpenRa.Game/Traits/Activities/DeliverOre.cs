using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class DeliverOre : IActivity
	{
		public IActivity NextActivity { get; set; }

		bool isDone;
		Actor refinery;

		public DeliverOre() { }

		public DeliverOre( Actor refinery )
		{
			this.refinery = refinery;
		}

		static readonly int2 refineryDeliverOffset = new int2( 1, 2 );

		public IActivity Tick( Actor self, Mobile mobile )
		{
			if( isDone )
			{
				self.traits.Get<Harvester>().Deliver( self );
				return NextActivity ?? new Harvest();
			}
			else if( NextActivity != null )
				return NextActivity;
			else if( refinery == null || refinery.IsDead || self.Location != refinery.Location + refineryDeliverOffset )
			{
				var search = new PathSearch
				{
					heuristic = PathSearch.DefaultEstimator( self.Location ),
					umt = mobile.GetMovementType(),
					checkForBlocked = false,
				};
				var refineries = Game.world.Actors.Where( x => x.unitInfo == Rules.UnitInfo[ "proc" ] ).ToList();
				foreach( var r in refineries )
					search.AddInitialCell( r.Location + refineryDeliverOffset );

				var path = Game.PathFinder.FindPath( search );
				path.Reverse();
				if( path.Count != 0 )
				{
					refinery = refineries.FirstOrDefault( x => x.Location + refineryDeliverOffset == path[ 0 ] );
					return new Move( () => path ) { NextActivity = this };
				}
				else
					// no refineries reachable?
					return null;
			}
			else if( mobile.facing != 64 )
				return new Turn( 64 ) { NextActivity = this };

			var renderUnit = self.traits.WithInterface<RenderUnit>().First();
			if( renderUnit.anim.CurrentSequence.Name != "empty" )
				renderUnit.PlayCustomAnimation( self, "empty",
					() => isDone = true );

			return null;
		}

		public void Cancel(Actor self, Mobile mobile)
		{
			// TODO: allow canceling of deliver orders?
		}
	}
}
