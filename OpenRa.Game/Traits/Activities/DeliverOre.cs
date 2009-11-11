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

		public void Tick(Actor self, Mobile mobile)
		{
			if( isDone )
			{
				var harv = self.traits.Get<Harvester>();

				harv.Deliver( self );

				if( NextActivity == null )
					NextActivity = new Harvest();
				mobile.InternalSetActivity( NextActivity );
				return;
			}
			else if( refinery == null || refinery.IsDead )
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
					var move = new Move( () => path );
					mobile.InternalSetActivity( move );
					mobile.QueueActivity( this );
					move.Tick( self, mobile );
					return;
				}
				else
					// no refineries reachable?
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

			var renderUnit = self.traits.WithInterface<RenderUnit>().First();
			if( renderUnit.anim.CurrentSequence.Name != "empty" )
				renderUnit.PlayCustomAnimation( self, "empty",
					() => isDone = true );
		}

		public void Cancel(Actor self, Mobile mobile)
		{
			mobile.InternalSetActivity(null);
		}
	}
}
