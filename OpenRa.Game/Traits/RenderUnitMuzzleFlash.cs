using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
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
			var attack = self.traits.WithInterface<AttackBase>().First();

			var muzzleFlash = new Animation(self.LegacyInfo.Name);
			muzzleFlash.PlayFetchIndex("muzzle",
				() => (Util.QuantizeFacing(unit.Facing, 8)) * 6 + (int)(attack.primaryRecoil * 5.9f));
			anims.Add( "muzzle", new AnimationWithOffset(
				muzzleFlash,
				() => self.LegacyInfo.PrimaryOffset.AbsOffset(),
				() => attack.primaryRecoil <= 0 ) );
		}
	}
}
