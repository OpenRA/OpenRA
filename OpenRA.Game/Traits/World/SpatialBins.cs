using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace OpenRA.Traits
{
	class SpatialBinsInfo : ITraitInfo
	{
		public readonly int BinSize = 8;
		public object Create(Actor self) { return new SpatialBins( self, this ); }
	}

	class SpatialBins : ITick
	{
		List<Actor>[,] bins;
		int scale;

		public SpatialBins(Actor self, SpatialBinsInfo info)
		{
			bins = new List<Actor>[
				self.World.Map.MapSize / info.BinSize,
				self.World.Map.MapSize / info.BinSize];

			scale = Game.CellSize * info.BinSize;
		}

		public void Tick(Actor self)
		{
			for (var j = 0; j < bins.GetUpperBound(1); j++)
				for (var i = 0; i < bins.GetUpperBound(0); i++)
					bins[i, j] = new List<Actor>();

			foreach (var a in self.World.Actors)
			{
				if (a.Location.X >= self.World.Map.MapSize
					|| a.Location.Y >= self.World.Map.MapSize)
					continue;

				var bounds = a.GetBounds(true);
				var i1 = (int)bounds.Left / scale;
				var i2 = (int)bounds.Right / scale;
				var j1 = (int)bounds.Top / scale;
				var j2 = (int)bounds.Bottom / scale;

				for (var j = j1; j <= j2; j++)
					for (var i = i1; i <= i2; i++)
						bins[i, j].Add(a);
			}
		}

		IEnumerable<Actor> ActorsInBins(int i1, int i2, int j1, int j2)
		{
			j1 = Math.Max(0, j1); j2 = Math.Min(j2, bins.GetUpperBound(1) - 1);
			i1 = Math.Max(0, i1); i2 = Math.Min(i2, bins.GetUpperBound(0) - 1);
			for (var j = j1; j <= j2; j++)
				for (var i = i1; i <= i2; i++)
					foreach (var a in bins[i, j])
						yield return a;
		}

		public IEnumerable<Actor> ActorsInBox(int2 a, int2 b)
		{
			var r = RectangleF.FromLTRB(a.X, a.Y, b.X, b.Y);

			return ActorsInBins(a.X / scale, a.Y / scale, b.X / scale, b.Y / scale)
				.Distinct()
				.Where(u => u.GetBounds(true).IntersectsWith(r));
		}
	}
}
