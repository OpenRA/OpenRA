using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class TeslaInstantKills : INotifyDamage
	{
		public void Damaged( Actor self, AttackInfo e )
		{
			if( e.Warhead.InfDeath == 5 )
				self.Health = 0;
		}
	}
}
