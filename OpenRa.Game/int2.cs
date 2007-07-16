using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.Game
{
	struct int2
	{
		public int X,Y;

		public int2( int x, int y ) { this.X = x; this.Y = y; }
		public int2( Point p ) { X = p.X; Y = p.Y; }
		public int2( Size p ) { X = p.Width; Y = p.Height; }

		public static int2 operator +(int2 a, int2 b) { return new int2(a.X + b.X, a.Y + b.Y); }
		public static int2 operator -(int2 a, int2 b) { return new int2(a.X - b.X, a.Y - b.Y); }
		public static int2 operator *(int a, int2 b) { return new int2(a * b.X, a * b.Y); }
		public static int2 operator *(int2 b, int a) { return new int2(a * b.X, a * b.Y); }

		public float2 ToFloat2() { return new float2(X, Y); }

		public static bool operator ==(int2 me, int2 other) { return (me.X == other.X && me.Y == other.Y); }
		public static bool operator !=(int2 me, int2 other) { return !(me == other); }

		public int2 Sign() { return new int2(Math.Sign(X), Math.Sign(Y)); }
		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			int2 o = (int2)obj;
			return o == this;
		}
	}
}
