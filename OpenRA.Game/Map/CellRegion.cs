#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections;
using System.Collections.Generic;

namespace OpenRA
{
	// Represents a (on-screen) rectangular collection of tiles.
	// TopLeft and BottomRight are inclusive
	public class CellRegion : IEnumerable<CPos>
	{
		// Corners of the region
		public readonly CPos TopLeft;
		public readonly CPos BottomRight;
		readonly TileShape shape;

		// Corners in map coordinates
		// These will only equal TopLeft and BottomRight for TileShape.Rectangular
		readonly CPos mapTopLeft;
		readonly CPos mapBottomRight;

		public CellRegion(TileShape shape, CPos topLeft, CPos bottomRight)
		{
			this.shape = shape;
			TopLeft = topLeft;
			BottomRight = bottomRight;

			mapTopLeft = Map.CellToMap(shape, TopLeft);
			mapBottomRight = Map.CellToMap(shape, BottomRight);
		}

		/// <summary>Expand the specified region with an additional cordon. This may expand the region outside the map borders.</summary>
		public static CellRegion Expand(CellRegion region, int cordon)
		{
			var offset = new CVec(cordon, cordon);
			var tl = Map.MapToCell(region.shape, Map.CellToMap(region.shape, region.TopLeft) - offset);
			var br = Map.MapToCell(region.shape, Map.CellToMap(region.shape, region.BottomRight) + offset);

			return new CellRegion(region.shape, tl, br);
		}

		public bool Contains(CPos cell)
		{
			var uv = Map.CellToMap(shape, cell);
			return uv.X >= mapTopLeft.X && uv.X <= mapBottomRight.X && uv.Y >= mapTopLeft.Y && uv.Y <= mapBottomRight.Y;
		}

		public IEnumerator<CPos> GetEnumerator()
		{
			return new CellRegionEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		class CellRegionEnumerator : IEnumerator<CPos>
		{
			readonly CellRegion r;

			// Current position, in map coordinates
			int u, v;

			public CellRegionEnumerator(CellRegion region)
			{
				r = region;
				Reset();
			}

			public bool MoveNext()
			{
				u += 1;

				// Check for column overflow
				if (u > r.mapBottomRight.X)
				{
					v += 1;
					u = r.mapTopLeft.X;

					// Check for row overflow
					if (v > r.mapBottomRight.Y)
						return false;
				}

				return true;
			}

			public void Reset()
			{
				// Enumerator starts *before* the first element in the sequence.
				u = r.mapTopLeft.X - 1;
				v = r.mapTopLeft.Y;
			}

			public CPos Current { get { return Map.MapToCell(r.shape, new CPos(u, v)); } }
			object IEnumerator.Current { get { return Current; } }
			public void Dispose() { }
		}
	}
}
