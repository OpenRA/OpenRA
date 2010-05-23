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

using System.Linq;

namespace OpenRA.Traits
{
	class ProvidesRadarInfo : TraitInfo<ProvidesRadar> { }

	class ProvidesRadar : ITick
	{
		public bool IsActive { get; private set; }

		public void Tick(Actor self) { IsActive = UpdateActive(self); }

		bool UpdateActive(Actor self)
		{
			// Check if powered
			var b = self.traits.Get<Building>();
			if (b.Disabled) return false;

			var isJammed = self.World.Queries.WithTrait<JamsRadar>().Any(a => self.Owner != a.Actor.Owner
				&& (self.Location - a.Actor.Location).Length < a.Actor.Info.Traits.Get<JamsRadarInfo>().Range);

			return !isJammed;
		}
	}

	class JamsRadarInfo : TraitInfo<JamsRadar> { public readonly int Range = 0;	}

	class JamsRadar { }
}
