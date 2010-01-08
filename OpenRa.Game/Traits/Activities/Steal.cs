using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Steal : IActivity
	{
		Actor target;
		
		public Steal(Actor target) { this.target = target; }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead) return NextActivity;
			if (target.Owner == self.Owner) return NextActivity;
			
			foreach (var t in target.traits.WithInterface<IAcceptThief>())
				t.OnSteal(target, self);
			
			return NextActivity;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
