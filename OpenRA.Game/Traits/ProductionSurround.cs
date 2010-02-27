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
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	class ProductionSurroundInfo : ProductionInfo
	{
		public override object Create(Actor self) { return new ProductionSurround(self); }
	}

	class ProductionSurround : Production
	{
		public ProductionSurround(Actor self) : base(self) { }

		static int2? FindAdjacentTile(Actor self, UnitMovementType umt)
		{
			var tiles = Footprint.Tiles(self, self.traits.Get<Traits.Building>());
			var min = tiles.Aggregate(int2.Min) - new int2(1, 1);
			var max = tiles.Aggregate(int2.Max) + new int2(1, 1);

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (self.World.IsCellBuildable(new int2(i, j), umt))
						return new int2(i, j);

			return null;
		}

		public override int2? CreationLocation(Actor self, ActorInfo producee)
		{
			return FindAdjacentTile(self, producee.Traits.Get<OwnedActorInfo>().WaterBound ?
					UnitMovementType.Float : UnitMovementType.Wheel);	/* hackety hack */
		}

		public override int CreationFacing(Actor self, Actor newUnit)
		{
			return Util.GetFacing(newUnit.CenterLocation - self.CenterLocation, 128);
		}
	}
}
