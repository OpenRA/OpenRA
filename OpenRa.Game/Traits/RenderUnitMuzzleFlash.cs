using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitMuzzleFlash : RenderUnit
	{
		public RenderUnitMuzzleFlash(Actor self)
			: base(self)
		{
			if (!self.Info.MuzzleFlash) throw new InvalidOperationException("wtf??");

			var unit = self.traits.Get<Unit>();
			var attack = self.traits.WithInterface<AttackBase>().First();

			var muzzleFlash = new Animation(self.Info.Name);
			muzzleFlash.PlayFetchIndex("muzzle",
				() => (Util.QuantizeFacing(unit.Facing, 8)) * 6 + (int)(attack.primaryRecoil * 5.9f));
			anims.Add( "muzzle", new AnimationWithOffset(
				muzzleFlash,
				() => new float2( self.Info.PrimaryOffset.ElementAtOrDefault( 2 ), self.Info.PrimaryOffset.ElementAtOrDefault( 3 ) ),
				() => attack.primaryRecoil <= 0 ) );
		}
	}
}
