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
	public readonly struct MapCoordsRegion : IEnumerable<MPos>
	{
		public struct MapCoordsEnumerator : IEnumerator<MPos>
		{
			readonly MapCoordsRegion r;

			public MapCoordsEnumerator(MapCoordsRegion region)
				: this()
			{
				r = region;
				Reset();
			}

			public bool MoveNext()
			{
				var u = Current.U + 1;
				var v = Current.V;

				// Check for column overflow
				if (u > r.BottomRight.U)
				{
					v += 1;
					u = r.TopLeft.U;

					// Check for row overflow
					if (v > r.BottomRight.V)
						return false;
				}

				Current = new MPos(u, v);
				return true;
			}

			public void Reset()
			{
				Current = new MPos(r.TopLeft.U - 1, r.TopLeft.V);
			}

			public MPos Current { get; private set; }
			object IEnumerator.Current => Current;
			public void Dispose() { }
		}

		public MapCoordsRegion(MPos mapTopLeft, MPos mapBottomRight)
		{
			TopLeft = mapTopLeft;
			BottomRight = mapBottomRight;
		}

		public override string ToString()
		{
			return $"{TopLeft}->{BottomRight}";
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

		public MPos TopLeft { get; }
		public MPos BottomRight { get; }
	}
}
