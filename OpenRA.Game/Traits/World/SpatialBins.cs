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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRA.Traits
{
	class SpatialBinsInfo : ITraitInfo
	{
		public readonly int BinSize = 8;
		public object Create(ActorInitializer init) { return new SpatialBins( init.self, this ); }
	}

	class SpatialBins : ITick
	{
		List<Actor>[,] bins;
		int scale;

		public SpatialBins(Actor self, SpatialBinsInfo info)
		{
			bins = new List<Actor>[
				self.World.Map.MapSize.X / info.BinSize,
				self.World.Map.MapSize.Y / info.BinSize];

			scale = Game.CellSize * info.BinSize;
		}

		public void Tick(Actor self)
		{
			for (var j = 0; j <= bins.GetUpperBound(1); j++)
				for (var i = 0; i <= bins.GetUpperBound(0); i++)
					bins[i, j] = new List<Actor>();

			foreach (var a in self.World.Actors)
			{
				var bounds = a.GetBounds(true);

				if (bounds.Right <= Game.CellSize * self.World.Map.XOffset) continue;
				if (bounds.Bottom <= Game.CellSize * self.World.Map.YOffset) continue;
				if (bounds.Left >= Game.CellSize * (self.World.Map.XOffset + self.World.Map.Width)) continue;
				if (bounds.Top >= Game.CellSize * (self.World.Map.YOffset + self.World.Map.Height)) continue;

				var i1 = Math.Max(0, (int)bounds.Left / scale);
				var i2 = Math.Min(bins.GetUpperBound(0), (int)bounds.Right / scale);
				var j1 = Math.Max(0, (int)bounds.Top / scale);
				var j2 = Math.Min(bins.GetUpperBound(1), (int)bounds.Bottom / scale);
				
				for (var j = j1; j <= j2; j++)
					for (var i = i1; i <= i2; i++)
						bins[i, j].Add(a);
			}
		}

		IEnumerable<Actor> ActorsInBins(int i1, int i2, int j1, int j2)
		{
			if (bins[0, 0] == null) yield break;	// hack

			j1 = Math.Max(0, j1); j2 = Math.Min(j2, bins.GetUpperBound(1));
			i1 = Math.Max(0, i1); i2 = Math.Min(i2, bins.GetUpperBound(0));
			for (var j = j1; j <= j2; j++)
				for (var i = i1; i <= i2; i++)
					foreach (var a in bins[i, j])
						yield return a;
		}

		public IEnumerable<Actor> ActorsInBox(int2 a, int2 b)
		{
			var r = RectangleF.FromLTRB(a.X, a.Y, b.X, b.Y);

			return ActorsInBins(a.X / scale, b.X / scale, a.Y / scale, b.Y / scale)
				.Distinct()
				.Where(u => u.GetBounds(true).IntersectsWith(r));
		}
	}
}
