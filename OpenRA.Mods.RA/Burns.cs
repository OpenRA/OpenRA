using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA
{
	class BurnsInfo : TraitInfo<Burns>
	{
		public readonly string Anim = "1";
		public readonly int Damage = 1;
		public readonly int Interval = 8;
	}

	class Burns : ITick
	{
		[Sync]
		int ticks;
		bool isSetup;

		public void Tick(Actor self)
		{
			if (!isSetup)
			{
				isSetup = true;

				var anim = new Animation("fire", () => 0);
				anim.PlayRepeating(self.Info.Traits.Get<BurnsInfo>().Anim);
				self.traits.Get<RenderSimple>().anims.Add("fire",
					new RenderSimple.AnimationWithOffset(anim, () => new float2(0, -3), null));
			}

			if (--ticks <= 0)
			{
				self.InflictDamage(self, self.Info.Traits.Get<BurnsInfo>().Damage, null);
				ticks = self.Info.Traits.Get<BurnsInfo>().Interval;
			}
		}
	}
}
