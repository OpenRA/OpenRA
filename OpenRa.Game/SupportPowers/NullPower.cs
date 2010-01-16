using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.SupportPowers
{
	class NullPower : ISupportPowerImpl
	{
		public void OnFireNotification(Actor a, int2 xy) { }
		public void IsReadyNotification(SupportPower p) { }
		public void IsChargingNotification(SupportPower p) { }
		public void Activate(SupportPower p)
		{
			// if this was a real power, i'd do something here!
			throw new NotImplementedException();
		}
	}
}
