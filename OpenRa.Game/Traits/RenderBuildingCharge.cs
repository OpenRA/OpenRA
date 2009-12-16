using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	/* used for tesla */
	class RenderBuildingCharge : RenderBuilding, INotifyAttack
	{
		public RenderBuildingCharge(Actor self)
			: base(self)
		{
		}

		public void Attacking(Actor self)
		{
			var prev = anim.CurrentSequence.Name;
			Sound.Play("tslachg2.aud");
			anim.PlayThen("active", () => anim.PlayRepeating(prev));
		}
	}
}
