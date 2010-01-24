using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
	class RemoveSelf : IActivity
	{
		bool isCanceled;
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			self.World.AddFrameEndTask(w => w.Remove(self));
			return null;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
