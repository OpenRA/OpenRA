using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Follow : IActivity
	{
		Actor Target;
		int Range;

		public Follow(Actor target, int range)
		{
			Target = target;
			Range = range;
		}

		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self, Mobile mobile )
		{
			if (Target == null || Target.IsDead)
				return NextActivity;

			if( ( Target.Location - self.Location ).LengthSquared >= Range * Range )
				return new Move( Target, Range ) { NextActivity = this };

			return null;
		}

		public void Cancel(Actor self, Mobile mobile)
		{
			Target = null;
		}
	}
}
