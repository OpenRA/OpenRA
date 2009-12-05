using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitMuzzleFlash : RenderUnit
	{
		Animation muzzleFlash;

		public RenderUnitMuzzleFlash(Actor self)
			: base(self)
		{
			if (!self.Info.MuzzleFlash) throw new InvalidOperationException("wtf??");

			muzzleFlash = new Animation(self.Info.Name);
			muzzleFlash.PlayFetchIndex("muzzle",
				() =>
				{
					var attack = self.traits.WithInterface<AttackBase>().First();
					var unit = self.traits.Get<Unit>();
					return (Util.QuantizeFacing(
						unit.Facing, 8)) * 6 + (int)(attack.primaryRecoil * 5.9f);
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
						self.Info.PrimaryOffset.ElementAtOrDefault(2),
						self.Info.PrimaryOffset.ElementAtOrDefault(3)))});
			else
				return base.Render(self);
		}
	}
}
