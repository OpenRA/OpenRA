#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public struct MapCoordsRegion : IEnumerable<MPos>
	{
		public struct MapCoordsEnumerator : IEnumerator<MPos>
		{
			readonly MapCoordsRegion r;
			MPos current;

			public MapCoordsEnumerator(MapCoordsRegion region)
				: this()
			{
				r = region;
				Reset();
			}

			public bool MoveNext()
			{
				var u = current.U + 1;
				var v = current.V;

				// Check for column overflow
				if (u > r.bottomRight.U)
				{
					v += 1;
					u = r.topLeft.U;

					// Check for row overflow
					if (v > r.bottomRight.V)
						return false;
				}

				current = new MPos(u, v);
				return true;
			}

			public void Reset()
			{
				current = new MPos(r.topLeft.U - 1, r.topLeft.V);
			}

			public MPos Current { get { return current; } }
			object IEnumerator.Current { get { return Current; } }
			public void Dispose() { }
		}

		readonly MPos topLeft;
		readonly MPos bottomRight;

		public MapCoordsRegion(MPos mapTopLeft, MPos mapBottomRight)
		{
			topLeft = mapTopLeft;
			bottomRight = mapBottomRight;
		}

		public MapCoordsEnumerator GetEnumerator()
		{
			return new MapCoordsEnumerator(this);
		}

		IEnumerator<MPos> IEnumerable<MPos>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public MPos TopLeft { get { return topLeft; } }
		public MPos BottomRight { get { return bottomRight; } }
	}
}
