using System;

namespace OpenRa.Traits.Activities
{
	class UndeployMcv : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool started;

		void DoUndeploy(World w,Actor self)
		{
			self.Health = 0;
			foreach (var ns in self.traits.WithInterface<INotifySold>())
				ns.Sold(self);
			w.Remove(self);
			
			var mcv = w.CreateActor("mcv", self.Location + new int2(1, 1), self.Owner);
			mcv.traits.Get<Unit>().Facing = 96;
		}

		public IActivity Tick(Actor self)
		{
			if (!started)
			{
				var rb = self.traits.Get<RenderBuilding>();
				rb.PlayCustomAnimBackwards(self, "make",
					() => Game.world.AddFrameEndTask(w => DoUndeploy(w,self)));

				Sound.Play("cashturn.aud");
				started = true;
			}

			return this;
		}

		public void Cancel(Actor self)
		{
			// Cancel can't happen between this being moved to the head of the list, and it being Ticked.
			throw new InvalidOperationException("UndeployMcvAction: Cancel() should never occur.");
		}
	}
}
