using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Harvest : Activity
	{
		public Activity NextActivity { get; set; }
		bool isHarvesting = false;

		public void Tick(Actor self, Mobile mobile)
		{
			if( NextActivity != null )
			{
				mobile.InternalSetActivity( NextActivity );
				NextActivity.Tick( self, mobile );
				return;
			}

			var harv = self.traits.Get<Harvester>();
			var isGem = false;

			if (!harv.IsFull &&
				Game.map.ContainsResource(self.Location) &&
				Game.map.Harvest(self.Location, out isGem))
			{
				var harvestAnim = "harvest" + Util.QuantizeFacing(mobile.facing, 8);
				var renderUnit = self.traits.WithInterface<RenderUnit>().First();	/* better have one of these! */
				if( harvestAnim != renderUnit.anim.CurrentSequence.Name )
				{
					isHarvesting = true;
					renderUnit.PlayCustomAnimation( self, harvestAnim, () => isHarvesting = false );
				}
				harv.AcceptResource(isGem);
				return;
			}

			if (isHarvesting) return;

			if (harv.IsFull)
				PlanReturnToBase(self, mobile);
			else
				PlanMoreHarvesting(self, mobile);
		}

		/* maybe this doesnt really belong here, since it's the
		 * same as what UnitOrders has to do for an explicit return */

		void PlanReturnToBase(Actor self, Mobile mobile)
		{
			/* find a proc */
			var proc = ChooseReturnLocation(self);
			if( proc != null )
			{
				mobile.QueueActivity( new Move( proc.Location + new int2( 1, 2 ), 0 ) );
				mobile.QueueActivity( new Turn( 64 ) );
				mobile.QueueActivity( new DeliverOre() );
			}
			mobile.InternalSetActivity(NextActivity);
		}

		static Actor ChooseReturnLocation(Actor self)
		{
			/* todo: compute paths to possible procs, taking into account enemy presence */
			/* currently, we're good at choosing close, inaccessible procs */

			return Game.world.Actors.Where(
				a => a.Owner == self.Owner &&
					 a.traits.Contains<AcceptsOre>())
					 .OrderBy(p => (p.Location - self.Location).LengthSquared)
					 .FirstOrDefault();
		}

		void PlanMoreHarvesting(Actor self, Mobile mobile)
		{
			/* find a nearby patch */
			/* todo: add the queries we need to support this! */

			var path = Game.PathFinder.FindUnitPath( self.Location, loc =>
				{
					if( Game.UnitInfluence.GetUnitAt( loc ) != null ) return double.PositiveInfinity;
					return Game.map.ContainsResource( loc ) ? 0 : 1;
				}, UnitMovementType.Wheel )
				.TakeWhile( a => a != self.Location )
				.ToList();
			if( path.Count != 0 )
			{
				mobile.QueueActivity( new Move( path ) );
				mobile.QueueActivity( new Harvest() );
			}

			mobile.InternalSetActivity(NextActivity);
		}

		public void Cancel(Actor self, Mobile mobile)
		{
		}
	}
}
