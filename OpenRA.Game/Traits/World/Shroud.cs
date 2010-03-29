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

using OpenRA.FileFormats;
using System.Collections.Generic;

namespace OpenRA.Traits
{
	class ShroudInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Shroud(self, this); }
	}

	class Shroud
	{
		Map map;
		int[,] visibleCells;

		public Shroud(Actor self, ShroudInfo info)
		{
			map = self.World.Map;
			visibleCells = new int[map.MapSize, map.MapSize];

			self.World.ActorAdded += AddActor;
			self.World.ActorRemoved += RemoveActor;
		}

		class ActorVisibility
		{
			int range;
			int2[] vis;
		}

		void AddActor(Actor a) { }
		void RemoveActor(Actor a) { }
	}
}
