#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Drawing;

namespace OpenRA
{
	public struct int2
	{
		public int X,Y;

		public int2( int x, int y ) { this.X = x; this.Y = y; }
		public int2( Point p ) { X = p.X; Y = p.Y; }
		public int2( Size p ) { X = p.Width; Y = p.Height; }

		public static int2 operator +(int2 a, int2 b) { return new int2(a.X + b.X, a.Y + b.Y); }
		public static int2 operator -(int2 a, int2 b) { return new int2(a.X - b.X, a.Y - b.Y); }
		public static int2 operator *(int a, int2 b) { return new int2(a * b.X, a * b.Y); }
		public static int2 operator *(int2 b, int a) { return new int2(a * b.X, a * b.Y); }

		public static bool operator ==(int2 me, int2 other) { return (me.X == other.X && me.Y == other.Y); }
		public static bool operator !=(int2 me, int2 other) { return !(me == other); }

		public int2 Sign() { return new int2(Math.Sign(X), Math.Sign(Y)); }
		public int2 Abs() { return new int2( Math.Abs( X ), Math.Abs( Y ) ); }
		public int LengthSquared { get { return X * X + Y * Y; } }
		public int Length { get { return (int)Math.Sqrt(LengthSquared); } }
		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public static int2 Max(int2 a, int2 b) { return new int2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static int2 Min(int2 a, int2 b) { return new int2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			int2 o = (int2)obj;
			return o == this;
		}

		public static readonly int2 Zero = new int2(0, 0);
		public Point ToPoint() { return new Point(X, Y); }
        public PointF ToPointF() { return new PointF(X, Y); }
		public float2 ToFloat2() { return new float2(X, Y); }

		public override string ToString() { return string.Format("{0},{1}", X, Y); }
	}
}
