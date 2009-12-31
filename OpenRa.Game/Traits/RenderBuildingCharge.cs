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
			var prefix = self.GetDamageState() == DamageState.Half ? "damaged-" : "";
			Sound.Play("tslachg2.aud");
			anim.PlayThen(prefix + "active", () => anim.PlayRepeating(prefix + "idle"));
		}
	}
}
