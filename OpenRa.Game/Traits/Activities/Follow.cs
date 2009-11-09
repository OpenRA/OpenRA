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

		public void Tick(Actor self, Mobile mobile)
		{
			if (Target.IsDead)
			{
				mobile.InternalSetActivity(NextActivity);
				return;
			}

			if ((Target.Location - self.Location).LengthSquared >= Range * Range)
			{
				mobile.InternalSetActivity(new Move(Target, Range));
				mobile.QueueActivity(this);
			}
		}

		public void Cancel(Actor self, Mobile mobile)
		{
			mobile.InternalSetActivity(null);
		}
	}
}
