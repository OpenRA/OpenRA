using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Traits;

namespace OpenRa.Mods.RA
{
	class InfiltrateForSonarPulseInfo : StatelessTraitInfo<InfiltrateForSonarPulse> { }
	
	class InfiltrateForSonarPulse : IAcceptSpy
	{
		public void OnInfiltrate(Actor self, Actor spy)
		{
			spy.Owner.PlayerActor.traits.Get<SonarPulsePower>().Give(1.0f);
		}
	}
}
