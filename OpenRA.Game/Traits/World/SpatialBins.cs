#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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

			for (var j = 0; j <= bins.GetUpperBound(1); j++)
				for (var i = 0; i <= bins.GetUpperBound(0); i++)
					bins[i, j] = new List<Actor>();
		}

		public void Tick(Actor self)
		{
			for (var j = 0; j <= bins.GetUpperBound(1); j++)
				for (var i = 0; i <= bins.GetUpperBound(0); i++)
					bins[i, j].Clear();

			foreach (var a in self.World.ActorsWithTrait<IOccupySpace>())
			{
				var bounds = a.Actor.ExtendedBounds.Value;

				if (bounds.Right <= Game.CellSize * self.World.Map.Bounds.Left) continue;
				if (bounds.Bottom <= Game.CellSize * self.World.Map.Bounds.Top) continue;
				if (bounds.Left >= Game.CellSize * self.World.Map.Bounds.Right) continue;
				if (bounds.Top >= Game.CellSize * self.World.Map.Bounds.Bottom) continue;

				var i1 = Math.Max(0, bounds.Left / scale);
				var i2 = Math.Min(bins.GetUpperBound(0), bounds.Right / scale);
				var j1 = Math.Max(0, bounds.Top / scale);
				var j2 = Math.Min(bins.GetUpperBound(1), bounds.Bottom / scale);

				for (var j = j1; j <= j2; j++)
					for (var i = i1; i <= i2; i++)
						bins[i, j].Add(a.Actor);
			}
		}

		IEnumerable<Actor> ActorsInBins(int i1, int i2, int j1, int j2)
		{
			j1 = Math.Max(0, j1); j2 = Math.Min(j2, bins.GetUpperBound(1));
			i1 = Math.Max(0, i1); i2 = Math.Min(i2, bins.GetUpperBound(0));
			for (var j = j1; j <= j2; j++)
				for (var i = i1; i <= i2; i++)
					foreach (var a in bins[i, j])
						yield return a;
		}

		public IEnumerable<Actor> ActorsInBox(PPos a, PPos b)
		{
			var r = Rectangle.FromLTRB(a.X, a.Y, b.X, b.Y);

			return ActorsInBins(a.X / scale, b.X / scale, a.Y / scale, b.Y / scale)
				.Distinct()
				.Where(u => u.IsInWorld && u.ExtendedBounds.Value.IntersectsWith(r));
		}
	}
}
