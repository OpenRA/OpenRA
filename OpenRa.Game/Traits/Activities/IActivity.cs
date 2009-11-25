using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	interface IActivity
	{
		IActivity NextActivity { get; set; }
		IActivity Tick( Actor self );
		void Cancel( Actor self );
	}
}
