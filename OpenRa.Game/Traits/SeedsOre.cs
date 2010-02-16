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

namespace OpenRa.Traits
{
	class SeedsOreInfo : ITraitInfo
	{
		public readonly float Chance = .05f;
		public readonly int Interval = 5;

		public object Create(Actor self) { return new SeedsOre(); }
	}

	class SeedsOre : ITick
	{
		int ticks;

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var info = self.Info.Traits.Get<SeedsOreInfo>();

				for (var j = -1; j < 2; j++)
					for (var i = -1; i < 2; i++)
						if (self.World.SharedRandom.NextDouble() < info.Chance)
							if (self.World.OreCanSpreadInto(self.Location.X + i, self.Location.Y + j))
								self.World.Map.AddOre(self.Location.X + i, self.Location.Y + j);

				ticks = info.Interval;
			}
		}
	}
}
