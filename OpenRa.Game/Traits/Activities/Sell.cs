using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Sell : IActivity
	{
		public IActivity NextActivity { get; set; }

		bool started;

		void DoSell(Actor self)
		{
			var refund = Rules.General.RefundPercent 
				* self.Health * self.Info.Cost / self.Info.Strength;

			self.Owner.GiveCash((int)refund);
			self.Health = 0;
			foreach (var ns in self.traits.WithInterface<INotifySold>())
				ns.Sold(self);
			Game.world.Remove(self);

			// todo: give dudes
		}

		public IActivity Tick(Actor self)
		{
			if (!started)
			{
				var rb = self.traits.Get<RenderBuilding>();
				rb.PlayCustomAnimBackwards(self, "make",
					() => Game.world.AddFrameEndTask(w => DoSell(self)));

				Sound.Play("cashturn.aud");
				started = true;
			}

			return this;
		}

		public void Cancel(Actor self) { /* never gonna give you up.. */ }
	}
}
