#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA
{
	// Represents a (on-screen) rectangular collection of tiles.
	// TopLeft and BottomRight are inclusive
	public class CellRegion : IEnumerable<CPos>
	{
		// Corners of the region
		public readonly CPos TopLeft;
		public readonly CPos BottomRight;
		readonly MapGridType gridType;

		// Corners in map coordinates
		// These will only equal TopLeft and BottomRight for MapGridType.Rectangular
		readonly MPos mapTopLeft;
		readonly MPos mapBottomRight;

		public CellRegion(MapGridType gridType, CPos topLeft, CPos bottomRight)
		{
			this.gridType = gridType;
			TopLeft = topLeft;
			BottomRight = bottomRight;

			mapTopLeft = TopLeft.ToMPos(gridType);
			mapBottomRight = BottomRight.ToMPos(gridType);
		}

		public CellRegion(MapGridType gridType, MPos topLeft, MPos bottomRight)
		{
			this.gridType = gridType;
			mapTopLeft = topLeft;
			mapBottomRight = bottomRight;

			TopLeft = topLeft.ToCPos(gridType);
			BottomRight = bottomRight.ToCPos(gridType);
		}

		public override string ToString()
		{
			return $"{TopLeft}->{BottomRight}";
		}

		/// <summary>Expand the specified region with an additional cordon. This may expand the region outside the map borders.</summary>
		public static CellRegion Expand(CellRegion region, int cordon)
		{
			var tl = new MPos(region.mapTopLeft.U - cordon, region.mapTopLeft.V - cordon).ToCPos(region.gridType);
			var br = new MPos(region.mapBottomRight.U + cordon, region.mapBottomRight.V + cordon).ToCPos(region.gridType);
			return new CellRegion(region.gridType, tl, br);
		}

		/// <summary>Returns the minimal region that covers at least the specified cells.</summary>
		public static CellRegion BoundingRegion(MapGridType shape, IEnumerable<CPos> cells)
		{
			if (cells == null || !cells.Any())
				throw new ArgumentException("cells must not be null or empty.", nameof(cells));

			var minU = int.MaxValue;
			var minV = int.MaxValue;
			var maxU = int.MinValue;
			var maxV = int.MinValue;
			foreach (var cell in cells)
			{
				var uv = cell.ToMPos(shape);
				if (minU > uv.U)
					minU = uv.U;
				if (maxU < uv.U)
					maxU = uv.U;
				if (minV > uv.V)
					minV = uv.V;
				if (maxV < uv.V)
					maxV = uv.V;
			}

			return new CellRegion(shape, new MPos(minU, minV).ToCPos(shape), new MPos(maxU, maxV).ToCPos(shape));
		}

		public bool Contains(CellRegion region)
		{
			return
				TopLeft.X <= region.TopLeft.X && TopLeft.Y <= region.TopLeft.Y &&
				BottomRight.X >= region.BottomRight.X && BottomRight.Y >= region.BottomRight.Y;
		}

		public bool Contains(CPos cell)
		{
			var uv = cell.ToMPos(gridType);
			return uv.U >= mapTopLeft.U && uv.U <= mapBottomRight.U && uv.V >= mapTopLeft.V && uv.V <= mapBottomRight.V;
		}

		public MapCoordsRegion MapCoords => new MapCoordsRegion(mapTopLeft, mapBottomRight);

		public CellRegionEnumerator GetEnumerator()
		{
			return new CellRegionEnumerator(this);
		}

		IEnumerator<CPos> IEnumerable<CPos>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public struct CellRegionEnumerator : IEnumerator<CPos>
		{
			readonly CellRegion r;

			// Current position, in map coordinates
			int u, v;

			// Current position, in cell coordinates
			CPos current;

			public CellRegionEnumerator(CellRegion region)
				: this()
			{
				r = region;
				Reset();
				current = new MPos(u, v).ToCPos(r.gridType);
			}

			public bool MoveNext()
			{
				u += 1;

				// Check for column overflow
				if (u > r.mapBottomRight.U)
				{
					v += 1;
					u = r.mapTopLeft.U;

					// Check for row overflow
					if (v > r.mapBottomRight.V)
						return false;
				}

				current = new MPos(u, v).ToCPos(r.gridType);
				return true;
			}

			public void Reset()
			{
				// Enumerator starts *before* the first element in the sequence.
				u = r.mapTopLeft.U - 1;
				v = r.mapTopLeft.V;
			}

			public CPos Current => current;
			object IEnumerator.Current => Current;
			public void Dispose() { }
		}
	}
}
