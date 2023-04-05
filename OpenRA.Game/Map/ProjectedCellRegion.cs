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

using System.Collections;
using System.Collections.Generic;

namespace OpenRA
{
	// Represents a (on-screen) rectangular collection of tiles.
	// TopLeft and BottomRight are inclusive
	public class ProjectedCellRegion : IEnumerable<PPos>
	{
		// Corners of the region
		public readonly PPos TopLeft;
		public readonly PPos BottomRight;

		// Corners of the bounding map region that contains all the cells that
		// may be projected within this region.
		readonly MPos mapTopLeft;
		readonly MPos mapBottomRight;

		public ProjectedCellRegion(Map map, PPos topLeft, PPos bottomRight)
		{
			TopLeft = topLeft;
			BottomRight = bottomRight;

			// The projection from MPos -> PPos cannot produce a larger V coordinate
			// so the top edge of the MPos region is the same as the PPos region.
			// (in fact the cells are identical if height == 0)
			mapTopLeft = (MPos)topLeft;

			// The bottom edge is trickier: cells at MPos.V > bottomRight.V may have
			// been projected into this region if they have height > 0.
			// Each height step is equivalent to 512 WDist units, which is one MPos
			// step for isometric cells, but only half a MPos step for classic cells. Doh!
			var maxHeight = map.Grid.MaximumTerrainHeight;
			var heightOffset = map.Grid.Type == MapGridType.RectangularIsometric ? maxHeight : maxHeight / 2;

			// Use the map Height data array to clamp the bottom coordinate so it doesn't overflow the map
			mapBottomRight = map.Height.Clamp(new MPos(bottomRight.U, bottomRight.V + heightOffset));
		}

		public bool Contains(PPos puv)
		{
			return puv.U >= TopLeft.U && puv.U <= BottomRight.U && puv.V >= TopLeft.V && puv.V <= BottomRight.V;
		}

		/// <summary>
		/// The region in map coordinates that contains all the cells that
		/// may be projected inside this region.  For increased performance,
		/// this does not validate whether individual map cells are actually
		/// projected inside the region.
		/// </summary>
		public MapCoordsRegion CandidateMapCoords => new(mapTopLeft, mapBottomRight);

		public ProjectedCellRegionEnumerator GetEnumerator()
		{
			return new ProjectedCellRegionEnumerator(this);
		}

		IEnumerator<PPos> IEnumerable<PPos>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public struct ProjectedCellRegionEnumerator : IEnumerator<PPos>
		{
			readonly ProjectedCellRegion r;

			// Current position, in projected map coordinates
			int u, v;

			public ProjectedCellRegionEnumerator(ProjectedCellRegion region)
				: this()
			{
				r = region;
				Reset();
				Current = new PPos(u, v);
			}

			public bool MoveNext()
			{
				u += 1;

				// Check for column overflow
				if (u > r.BottomRight.U)
				{
					v += 1;
					u = r.TopLeft.U;

					// Check for row overflow
					if (v > r.BottomRight.V)
						return false;
				}

				Current = new PPos(u, v);
				return true;
			}

			public void Reset()
			{
				// Enumerator starts *before* the first element in the sequence.
				u = r.TopLeft.U - 1;
				v = r.TopLeft.V;
			}

			public PPos Current { get; private set; }
			object IEnumerator.Current => Current;
			public void Dispose() { }
		}
	}
}
