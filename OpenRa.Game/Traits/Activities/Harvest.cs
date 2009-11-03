using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Harvest : Activity
	{
		public Activity NextActivity { get; set; }

		public void Tick(Actor self, Mobile mobile)
		{
			var harv = self.traits.Get<Harvester>();
			var isGem = false;

			if (!harv.IsFull && 
				Game.map.ContainsResource(self.Location) && 
				Game.map.Harvest(self.Location, out isGem))
			{
				harv.AcceptResource(isGem);
				return;
			}

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
			var proc = Game.world.Actors.Where(
				a => a.Owner == self.Owner &&
					 a.traits.Contains<AcceptsOre>())
					 .FirstOrDefault();		/* todo: *closest* proc, maybe? */

			if (proc == null)
			{
				Cancel(self, mobile);		/* is this a sane way to cancel? */
				return;
			}

			mobile.QueueActivity(new Move(proc.Location + new int2(1, 2)));
			mobile.QueueActivity(new Turn(64));
			/* todo: DeliverOre activity */

			mobile.InternalSetActivity(NextActivity);
		}

		void PlanMoreHarvesting(Actor self, Mobile mobile)
		{
			/* find a nearby patch */
			/* todo: add the queries we need to support this! */

			mobile.InternalSetActivity(NextActivity);
		}

		public void Cancel(Actor self, Mobile mobile)
		{
			mobile.InternalSetActivity(null);	/* bob: anything else required? */
		}
	}
}
