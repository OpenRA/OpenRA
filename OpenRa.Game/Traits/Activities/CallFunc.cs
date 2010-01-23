using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
	public class CallFunc : IActivity
	{
		Action a;
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (a != null) a();
			return NextActivity;
		}

		public void Cancel(Actor self) { a = null; NextActivity = null; }
	}
}
