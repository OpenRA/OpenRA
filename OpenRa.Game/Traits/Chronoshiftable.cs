using OpenRa.Traits;
using OpenRa.Orders;
using System.Collections.Generic;
using System.Linq;

namespace OpenRa.Traits
{
	class ChronoshiftableInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Chronoshiftable(self); }
	}

	public class Chronoshiftable : ITick
	{
		// Return-to-sender logic
		[Sync]
		int2 chronoshiftOrigin;
		[Sync]
		int chronoshiftReturnTicks = 0;

		public Chronoshiftable(Actor self) { }

		public void Tick(Actor self)
		{
			if (chronoshiftReturnTicks <= 0)
				return;

			if (chronoshiftReturnTicks > 0)
				chronoshiftReturnTicks--;

			// Return to original location
			if (chronoshiftReturnTicks == 0)
			{
				self.CancelActivity();
				// Todo: need a new Teleport method that will move to the closest available cell
				self.QueueActivity(new Activities.Teleport(chronoshiftOrigin));
			}
		}

		public virtual bool Activate(Actor self, int2 targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			/// Set up return-to-sender info
			chronoshiftOrigin = self.Location;
			chronoshiftReturnTicks = duration;
			
			// Kill cargo
			if (killCargo && self.traits.Contains<Cargo>())
			{
				var cargo = self.traits.Get<Cargo>();
				while (!cargo.IsEmpty(self))
				{
					chronosphere.Owner.Kills++;
					cargo.Unload(self);
				}
			}

			// Set up the teleport
			self.CancelActivity();
			self.QueueActivity(new Activities.Teleport(targetLocation));
			
			return true;
		}
	}
}
