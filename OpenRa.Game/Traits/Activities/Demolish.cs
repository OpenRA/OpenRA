using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Demolish : IActivity
	{
		Actor target;
		public IActivity NextActivity { get; set; }

		public Demolish( Actor target )
		{
			this.target = target;
		}

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead) return NextActivity;

			// 1. run to adj tile
			// 2. spawn timed demolition (for +3/4s)
			// 3. run away --- where?
			return this;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
