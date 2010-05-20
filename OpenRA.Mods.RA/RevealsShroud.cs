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
	class RevealsShroudInfo : TraitInfo<RevealsShroud> { }

	class RevealsShroud : ITick
	{
		int2 previousLocation;

		public void Tick(Actor self)
		{
			if (!self.IsIdle && previousLocation != self.Location)
			{
				previousLocation = self.Location;
				self.World.WorldActor.traits.Get<Shroud>().UpdateActor(self);
			}
		}
	}
}
