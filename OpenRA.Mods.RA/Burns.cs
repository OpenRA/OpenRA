#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

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
				self.Trait<RenderSimple>().anims.Add("fire",
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
