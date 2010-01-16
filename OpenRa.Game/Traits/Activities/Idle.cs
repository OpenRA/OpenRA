using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
	class Idle : IActivity
	{
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self) { return NextActivity; }
		public void Cancel(Actor self) {}
	}
}
