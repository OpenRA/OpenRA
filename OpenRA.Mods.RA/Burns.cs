#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
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
