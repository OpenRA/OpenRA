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

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SelfHealingInfo : TraitInfo<SelfHealing>
	{
		public readonly int Step = 5;
		public readonly int Ticks = 5;
		public readonly float HealIfBelow = .5f;
	}

	class SelfHealing : ITick
	{
		[Sync]
		int ticks;

		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<SelfHealingInfo>();

			if ((float)self.Health / self.GetMaxHP() >= info.HealIfBelow)
				return;

			if (--ticks <= 0)
			{
				ticks = info.Ticks;
				self.InflictDamage(self, -info.Step, null);
			}
		}
	}
}
