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

namespace OpenRA
{
	public readonly struct CellCoordsRegion : IEnumerable<CPos>
	{
		public struct CellCoordsEnumerator : IEnumerator<CPos>
		{
			readonly CellCoordsRegion r;

			public CellCoordsEnumerator(CellCoordsRegion region)
				: this()
			{
				r = region;
				Reset();
			}

			public bool MoveNext()
			{
				var x = Current.X + 1;
				var y = Current.Y;

				// Check for column overflow.
				if (x > r.BottomRight.X)
				{
					y++;
					x = r.TopLeft.X;

					// Check for row overflow.
					if (y > r.BottomRight.Y)
						return false;
				}

				Current = new CPos(x, y);
				return true;
			}

			public void Reset()
			{
				Current = new CPos(r.TopLeft.X - 1, r.TopLeft.Y);
			}

			public CPos Current { get; private set; }

			readonly object IEnumerator.Current => Current;
			public readonly void Dispose() { }
		}

		public CellCoordsRegion(CPos topLeft, CPos bottomRight)
		{
			TopLeft = topLeft;
			BottomRight = bottomRight;
		}

		public bool Contains(CPos cell)
		{
			return cell.X >= TopLeft.X && cell.X <= BottomRight.X && cell.Y >= TopLeft.Y && cell.Y <= BottomRight.Y;
		}

		public override string ToString()
		{
			return $"{TopLeft}->{BottomRight}";
		}

		public CellCoordsEnumerator GetEnumerator()
		{
			return new CellCoordsEnumerator(this);
		}

		IEnumerator<CPos> IEnumerable<CPos>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>Returns the minimal region that covers at least the specified cells.</summary>
		public static CellCoordsRegion BoundingRegion(IReadOnlyCollection<CPos> cells)
		{
			if (cells == null || cells.Count == 0)
				throw new ArgumentException("cells must not be null or empty.", nameof(cells));

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

			return new CellCoordsRegion(new CPos(minX, minY), new CPos(maxX, maxY));
		}

		public CPos TopLeft { get; }
		public CPos BottomRight { get; }
	}
}
