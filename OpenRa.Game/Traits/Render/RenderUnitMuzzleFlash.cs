using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Graphics;

namespace OpenRa.Traits
{
	class RenderUnitMuzzleFlashInfo : RenderUnitInfo
	{
		public override object Create(Actor self) { return new RenderUnitMuzzleFlash(self); }
	}

	class RenderUnitMuzzleFlash : RenderUnit
	{
		public RenderUnitMuzzleFlash(Actor self)
			: base(self)
		{
			var unit = self.traits.Get<Unit>();
			var attack = self.traits.Get<AttackBase>();
			var attackInfo = self.Info.Traits.Get<AttackBaseInfo>();

			var muzzleFlash = new Animation(GetImage(self));
			muzzleFlash.PlayFetchIndex("muzzle",
				() => (Util.QuantizeFacing(unit.Facing, 8)) * 6 + (int)(attack.primaryRecoil * 5.9f));
			anims.Add( "muzzle", new AnimationWithOffset(
				muzzleFlash,
				() => attackInfo.PrimaryOffset.AbsOffset(),
				() => attack.primaryRecoil <= 0 ) );
		}
	}
}
