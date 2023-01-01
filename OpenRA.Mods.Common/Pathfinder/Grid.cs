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

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// Represents a simplistic grid of cells, where everything in the
	/// top-to-bottom and left-to-right range is within the grid.
	/// The grid can be restricted to a single layer, or allowed to span all layers.
	/// </summary>
	/// <remarks>
	/// This means in <see cref="MapGridType.RectangularIsometric"/> some cells within a grid may lay off the map.
	/// Contrast this with <see cref="CellRegion"/> which maintains the simplistic grid in map space -
	/// ensuring the cells are therefore always within the map area.
	/// The advantage of Grid is that it has straight edges, making logic for adjacent grids easy.
	/// A CellRegion has jagged edges in RectangularIsometric, which makes that more difficult.
	/// </remarks>
	public readonly struct Grid
	{
		/// <summary>
		/// Inclusive.
		/// </summary>
		public readonly CPos TopLeft;

		/// <summary>
		/// Exclusive.
		/// </summary>
		public readonly CPos BottomRight;

		/// <summary>
		/// When true, the grid spans only the single layer given by the cells. When false, it spans all layers.
		/// </summary>
		public readonly bool SingleLayer;

		public Grid(CPos topLeft, CPos bottomRight, bool singleLayer)
		{
			if (topLeft.Layer != bottomRight.Layer)
				throw new ArgumentException($"{nameof(topLeft)} and {nameof(bottomRight)} must have the same {nameof(CPos.Layer)}");

			TopLeft = topLeft;
			BottomRight = bottomRight;
			SingleLayer = singleLayer;
		}

		public int Width => BottomRight.X - TopLeft.X;
		public int Height => BottomRight.Y - TopLeft.Y;

		/// <summary>
		/// Checks if the cell X and Y lie within the grid bounds. The cell layer must also match.
		/// </summary>
		public bool Contains(CPos cell)
		{
			return
				cell.X >= TopLeft.X && cell.X < BottomRight.X &&
				cell.Y >= TopLeft.Y && cell.Y < BottomRight.Y &&
				(!SingleLayer || cell.Layer == TopLeft.Layer);
		}

		/// <summary>
		/// Checks if the line segment from <paramref name="start"/> to <paramref name="end"/>
		/// passes through the grid boundary. The cell layers are ignored.
		/// A line contained wholly within the grid that doesn't cross the boundary is not counted as intersecting.
		/// </summary>
		public bool IntersectsLine(CPos start, CPos end)
		{
			var s = new int2(start.X, start.Y);
			var e = new int2(end.X, end.Y);
			var tl = new int2(TopLeft.X, TopLeft.Y);
			var tr = new int2(BottomRight.X, TopLeft.Y);
			var bl = new int2(TopLeft.X, BottomRight.Y);
			var br = new int2(BottomRight.X, BottomRight.Y);
			return
				Exts.LinesIntersect(s, e, tl, tr) ||
				Exts.LinesIntersect(s, e, tl, bl) ||
				Exts.LinesIntersect(s, e, bl, br) ||
				Exts.LinesIntersect(s, e, tr, br);
		}

		public override string ToString()
		{
			return $"{TopLeft}->{BottomRight}";
		}
	}
}
