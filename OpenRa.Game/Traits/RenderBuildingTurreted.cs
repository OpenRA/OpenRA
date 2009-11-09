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
			Make( () => PlayTurretAnim( self, "idle" ), self);
		}

		void PlayTurretAnim(Actor self, string a)
		{
			anim.PlayFetchIndex(a,
				() => self.traits.Get<Turreted>().turretFacing / 8);
		}

		public override void Damaged(Actor self, DamageState ds)
		{
			switch (ds)
			{
				case DamageState.Normal:
					PlayTurretAnim(self, "idle");
					break;
				case DamageState.Half:
					PlayTurretAnim(self, "damaged-idle");
					Game.PlaySound("kaboom1.aud", false);
					break;
			}
		}
	}
}
