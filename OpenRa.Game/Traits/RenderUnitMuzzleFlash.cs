using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitMuzzleFlash : RenderUnit
	{
		Animation muzzleFlash;

		public RenderUnitMuzzleFlash(Actor self)
			: base(self)
		{
			if (!self.unitInfo.MuzzleFlash) throw new InvalidOperationException("wtf??");

			muzzleFlash = new Animation(self.unitInfo.Name);
			muzzleFlash.PlayFetchIndex("muzzle",
				() =>
				{
					var attack = self.traits.WithInterface<AttackBase>().First();
					var mobile = self.traits.WithInterface<Mobile>().First();
					return (Util.QuantizeFacing(
						mobile.facing, 8)) * 6 + (int)(attack.primaryRecoil * 5.9f);
				});
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			muzzleFlash.Tick();
		}

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var attack = self.traits.WithInterface<AttackBase>().First();
			if (attack.primaryRecoil > 0)
				return base.Render(self).Concat(new[] {Util.Centered(self,
					muzzleFlash.Image, self.CenterLocation + new float2(
						self.unitInfo.PrimaryOffset.ElementAtOrDefault(2),
						self.unitInfo.PrimaryOffset.ElementAtOrDefault(3)))});
			else
				return base.Render(self);
		}
	}
}
