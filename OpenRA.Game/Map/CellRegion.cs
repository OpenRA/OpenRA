#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		readonly TileShape shape;

		// Corners in map coordinates
		// These will only equal TopLeft and BottomRight for TileShape.Rectangular
		readonly MPos mapTopLeft;
		readonly MPos mapBottomRight;

		public CellRegion(TileShape shape, CPos topLeft, CPos bottomRight)
		{
			this.shape = shape;
			TopLeft = topLeft;
			BottomRight = bottomRight;

			mapTopLeft = TopLeft.ToMPos(shape);
			mapBottomRight = BottomRight.ToMPos(shape);
		}

		/// <summary>Expand the specified region with an additional cordon. This may expand the region outside the map borders.</summary>
		public static CellRegion Expand(CellRegion region, int cordon)
		{
			var tl = new MPos(region.mapTopLeft.U - cordon, region.mapTopLeft.V - cordon).ToCPos(region.shape);
			var br = new MPos(region.mapBottomRight.U + cordon, region.mapBottomRight.V + cordon).ToCPos(region.shape);
			return new CellRegion(region.shape, tl, br);
		}

		/// <summary>Returns the minimal region that covers at least the specified cells.</summary>
		public static CellRegion BoundingRegion(TileShape shape, IEnumerable<CPos> cells)
		{
			if (cells == null || !cells.Any())
				throw new ArgumentException("cells must not be null or empty.", "cells");

			var minX = int.MaxValue;
			var minY = int.MaxValue;
			var maxX = int.MinValue;
			var maxY = int.MinValue;
			foreach (var cell in cells)
			{
				if (minX > cell.X)
					minX = cell.X;
				if (maxX < cell.X)
					maxX = cell.X;
				if (minY > cell.Y)
					minY = cell.Y;
				if (maxY < cell.Y)
					maxY = cell.Y;
			}

			return new CellRegion(shape, new CPos(minX, minY), new CPos(maxX, maxY));
		}

		public bool Contains(CellRegion region)
		{
			return
				TopLeft.X <= region.TopLeft.X && TopLeft.Y <= region.TopLeft.Y &&
				BottomRight.X >= region.BottomRight.X && BottomRight.Y >= region.BottomRight.Y;
		}

		public bool Contains(CPos cell)
		{
			var uv = cell.ToMPos(shape);
			return uv.U >= mapTopLeft.U && uv.U <= mapBottomRight.U && uv.V >= mapTopLeft.V && uv.V <= mapBottomRight.V;
		}

		public MapCoordsRegion MapCoords
		{
			get { return new MapCoordsRegion(mapTopLeft, mapBottomRight); }
		}

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

		public sealed class CellRegionEnumerator : IEnumerator<CPos>
		{
			readonly CellRegion r;

			// Current position, in map coordinates
			int u, v;

			// Current position, in cell coordinates
			CPos current;

			public CellRegionEnumerator(CellRegion region)
			{
				r = region;
				Reset();
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

				current = new MPos(u, v).ToCPos(r.shape);
				return true;
			}

			public void Reset()
			{
				// Enumerator starts *before* the first element in the sequence.
				u = r.mapTopLeft.U - 1;
				v = r.mapTopLeft.V;
			}

			public CPos Current { get { return current; } }
			object IEnumerator.Current { get { return Current; } }
			public void Dispose() { }
		}
	}
}
