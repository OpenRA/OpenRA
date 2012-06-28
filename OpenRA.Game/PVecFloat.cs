#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;

namespace OpenRA
{
	/// <summary>
	/// Pixel coordinate vector (fine; float)
	/// </summary>
	public struct PVecFloat
	{
		public readonly float X, Y;

		public PVecFloat(float x, float y) { X = x; Y = y; }
		public PVecFloat(Size p) { X = p.Width; Y = p.Height; }

		public static readonly PVecFloat Zero = new PVecFloat(0, 0);

		public static explicit operator PVecInt(PVecFloat a) { return new PVecInt((int)a.X, (int)a.Y); }
		public static explicit operator PVecFloat(float2 a) { return new PVecFloat(a.X, a.Y); }

		public static PVecFloat operator +(PVecFloat a, PVecFloat b) { return new PVecFloat(a.X + b.X, a.Y + b.Y); }
		public static PVecFloat operator -(PVecFloat a, PVecFloat b) { return new PVecFloat(a.X - b.X, a.Y - b.Y); }
		public static PVecFloat operator *(float a, PVecFloat b) { return new PVecFloat(a * b.X, a * b.Y); }
		public static PVecFloat operator *(PVecFloat b, float a) { return new PVecFloat(a * b.X, a * b.Y); }
		public static PVecFloat operator /(PVecFloat a, float b) { return new PVecFloat(a.X / b, a.Y / b); }

		public static PVecFloat operator -(PVecFloat a) { return new PVecFloat(-a.X, -a.Y); }

		public static bool operator ==(PVecFloat me, PVecFloat other) { return (me.X == other.X && me.Y == other.Y); }
		public static bool operator !=(PVecFloat me, PVecFloat other) { return !(me == other); }

		public static PVecFloat Max(PVecFloat a, PVecFloat b) { return new PVecFloat(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static PVecFloat Min(PVecFloat a, PVecFloat b) { return new PVecFloat(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public static float Dot(PVecFloat a, PVecFloat b) { return a.X * b.X + a.Y * b.Y; }

		public static PVecFloat FromAngle(float a) { return new PVecFloat((float)Math.Sin(a), (float)Math.Cos(a)); }

		public static PVecFloat Lerp(PVecFloat a, PVecFloat b, float t)
		{
			return new PVecFloat(
				float2.Lerp(a.X, b.X, t),
				float2.Lerp(a.Y, b.Y, t)
			);
		}

		public static PVecFloat Lerp(PVecFloat a, PVecFloat b, PVecFloat t)
		{
			return new PVecFloat(
				float2.Lerp(a.X, b.X, t.X),
				float2.Lerp(a.Y, b.Y, t.Y)
			);
		}

		public PVecFloat Sign() { return new PVecFloat(Math.Sign(X), Math.Sign(Y)); }
		public PVecFloat Abs() { return new PVecFloat(Math.Abs(X), Math.Abs(Y)); }
		public PVecFloat Round() { return new PVecFloat((float)Math.Round(X), (float)Math.Round(Y)); }
		public float LengthSquared { get { return X * X + Y * Y; } }
		public float Length { get { return (float)Math.Sqrt(LengthSquared); } }

		public float2 ToFloat2() { return new float2(X, Y); }
		public int2 ToInt2() { return new int2((int)X, (int)Y); }

		static float Constrain(float x, float a, float b) { return x < a ? a : x > b ? b : x; }

		public PVecFloat Constrain(PVecFloat min, PVecFloat max)
		{
			return new PVecFloat(
				Constrain(X, min.X, max.X),
				Constrain(Y, min.Y, max.Y)
			);
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			PVecFloat o = (PVecFloat)obj;
			return o == this;
		}

		public override string ToString() { return "({0},{1})".F(X, Y); }
	}
}
