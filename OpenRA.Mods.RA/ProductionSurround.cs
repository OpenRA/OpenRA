#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ProductionSurroundInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init) { return new ProductionSurround(); }
	}

	class ProductionSurround : Production
	{
		static int2? FindAdjacentTile(Actor self, bool waterBound)
		{
			var tiles = Footprint.Tiles(self);
			var min = tiles.Aggregate(int2.Min) - new int2(1, 1);
			var max = tiles.Aggregate(int2.Max) + new int2(1, 1);

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (self.World.IsCellBuildable(new int2(i, j), waterBound))
						return new int2(i, j);

			return null;
		}

		public override int2? CreationLocation(Actor self, ActorInfo producee)
		{
			return FindAdjacentTile(self, self.Info.Traits.Get<BuildingInfo>().WaterBound);
		}

		public override int CreationFacing(Actor self, Actor newUnit)
		{
			return Util.GetFacing(newUnit.CenterLocation - self.CenterLocation, 128);
		}
	}
}
