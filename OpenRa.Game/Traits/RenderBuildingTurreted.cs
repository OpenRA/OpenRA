using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class RenderBuildingTurreted : RenderBuilding
	{
		public RenderBuildingTurreted(Actor self)
			: base(self)
		{
			anim.PlayThen("make", () => anim.PlayFetchIndex("idle", 
				() => self.traits.Get<Turreted>().turretFacing));
		}
	}
}
