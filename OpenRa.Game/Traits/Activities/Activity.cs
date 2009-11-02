using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	interface Activity
	{
		Activity NextActivity { get; set; }
		void Tick( Actor self, Mobile mobile );
		void Cancel( Actor self, Mobile mobile );
	}
}
