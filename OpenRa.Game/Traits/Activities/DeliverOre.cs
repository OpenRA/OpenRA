using System.Linq;

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

		public IActivity Tick( Actor self )
		{
			var unit = self.traits.Get<Unit>();
			var mobile = self.traits.Get<Mobile>();

			if( isDone )
			{
				self.traits.Get<Harvester>().Deliver( self, refinery );
				return NextActivity ?? new Harvest();
			}
			else if( NextActivity != null )
				return NextActivity;

			if( refinery != null && refinery.IsDead )
				refinery = null;

			if( refinery == null || self.Location != refinery.Location + refineryDeliverOffset )
			{
				var search = new PathSearch
				{
					heuristic = PathSearch.DefaultEstimator( self.Location ),
					umt = mobile.GetMovementType(),
					checkForBlocked = false,
				};
				var refineries = Game.world.Actors.Where( x => x.traits.Contains<AcceptsOre>() 
					&& x.Owner == self.Owner ).ToList();
				if( refinery != null )
					search.AddInitialCell( refinery.Location + refineryDeliverOffset );
				else
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
					return this;
			}
			else if( unit.Facing != 64 )
				return new Turn( 64 ) { NextActivity = this };

			var renderUnit = self.traits.WithInterface<RenderUnit>().First();
			if( renderUnit.anim.CurrentSequence.Name != "empty" )
				renderUnit.PlayCustomAnimation( self, "empty",
					() => isDone = true );

			return this;
		}

		public void Cancel(Actor self)
		{
			// TODO: allow canceling of deliver orders?
		}
	}
}
