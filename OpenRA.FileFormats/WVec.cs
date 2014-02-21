#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;

namespace OpenRA
{
	/// <summary>
	/// 3d World vector for describing offsets and distances - 1024 units = 1 cell.
	/// </summary>
	public struct WVec
	{
		public readonly int X, Y, Z;

		public WVec(int x, int y, int z) { X = x; Y = y; Z = z; }
		public WVec(WDist x, WDist y, WDist z) { X = x.Range; Y = y.Range; Z = z.Range; }

		public static readonly WVec Zero = new WVec(0, 0, 0);

		public static WVec operator +(WVec a, WVec b) { return new WVec(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
		public static WVec operator -(WVec a, WVec b) { return new WVec(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
		public static WVec operator -(WVec a) { return new WVec(-a.X, -a.Y, -a.Z); }
		public static WVec operator /(WVec a, int b) { return new WVec(a.X / b, a.Y / b, a.Z / b); }
		public static WVec operator *(int a, WVec b) { return new WVec(a * b.X, a * b.Y, a * b.Z); }
		public static WVec operator *(WVec a, int b) { return b*a; }

		public static bool operator ==(WVec me, WVec other) { return (me.X == other.X && me.Y == other.Y && me.Z == other.Z); }
		public static bool operator !=(WVec me, WVec other) { return !(me == other); }

		public static int Dot(WVec a, WVec b) { return a.X * b.X + a.Y * b.Y + a.Z * b.Z; }
		public long LengthSquared { get { return (long)X * X + (long)Y * Y + (long)Z * Z; } }
		public int Length { get { return (int)Math.Sqrt(LengthSquared); } }
		public long HorizontalLengthSquared { get { return (long)X * X + (long)Y * Y; } }
		public int HorizontalLength { get { return (int)Math.Sqrt(HorizontalLengthSquared); } }

		public WVec Rotate(WRot rot)
		{
			var mtx = rot.AsMatrix();
			var lx = (long)X;
			var ly = (long)Y;
			var lz = (long)Z;
			return new WVec(
				(int)((lx * mtx[0] + ly*mtx[4] + lz*mtx[8]) / mtx[15]),
				(int)((lx * mtx[1] + ly*mtx[5] + lz*mtx[9]) / mtx[15]),
				(int)((lx * mtx[2] + ly*mtx[6] + lz*mtx[10]) / mtx[15]));
		}

		public static WVec Lerp(WVec a, WVec b, int mul, int div) { return a + (b - a) * mul / div; }

		public static WVec LerpQuadratic(WVec a, WVec b, WAngle pitch, int mul, int div)
		{
			// Start with a linear lerp between the points
			var ret = Lerp(a, b, mul, div);

			if (pitch.Angle == 0)
				return ret;

			// Add an additional quadratic variation to height
			// Uses fp to avoid integer overflow
			var offset = (int)((float)((float)(b - a).Length*pitch.Tan()*mul*(div - mul)) / (float)(1024*div*div));
			return new WVec(ret.X, ret.Y, ret.Z + offset);
		}

		// Sampled a N-sample probability density function in the range [-1024..1024, -1024..1024]
		// 1 sample produces a rectangular probability
		// 2 samples produces a triangular probability
		// ...
		// N samples approximates a true gaussian
		public static WVec FromPDF(Thirdparty.Random r, int samples)
		{
			return new WVec(WDist.FromPDF(r, samples), WDist.FromPDF(r, samples), WDist.Zero);
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode(); }

		public override bool Equals(object obj)
		{
			var o = obj as WVec?;
			return o != null && o == this;
		}

		public override string ToString() { return "{0},{1},{2}".F(X, Y, Z); }
	}
}
