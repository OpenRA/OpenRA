using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.SupportPowers
{
	class NullPower : ISupportPowerImpl
	{
		public void Activate(SupportPower p)
		{
			// if this was a real power, i'd do something here!
			throw new NotImplementedException();
		}
	}
}
