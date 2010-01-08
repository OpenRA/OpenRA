using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class StealOre : IActivity
	{
		Actor target;
		public const int CashStolen = 100; //todo: push this out to Rules

		public StealOre(Actor target) { this.target = target; }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead) return NextActivity;

			if (target.Owner == self.Owner) return NextActivity;

			target.Owner.TakeCash(CashStolen);
			self.Owner.GiveCash(CashStolen);

			// the thief is sacrificed.
			self.Health = 0;
			Game.world.AddFrameEndTask(w => w.Remove(self));
			
			return NextActivity;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
