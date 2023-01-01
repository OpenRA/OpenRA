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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using OpenRA.Primitives;

namespace OpenRA
{
	[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Mimic a built-in type alias.")]
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct float2 : IEquatable<float2>
	{
		public readonly float X, Y;

		public float2(float x, float y) { X = x; Y = y; }
		public float2(Size p) { X = p.Width; Y = p.Height; }
		public float2(int2 p) { X = p.X; Y = p.Y; }

		public static implicit operator float2(int2 src) { return new float2(src.X, src.Y); }

		public static float2 operator +(float2 a, float2 b) { return new float2(a.X + b.X, a.Y + b.Y); }
		public static float2 operator -(float2 a, float2 b) { return new float2(a.X - b.X, a.Y - b.Y); }

		public static float2 operator -(float2 a) { return new float2(-a.X, -a.Y); }

		public static float Lerp(float a, float b, float t) { return a + t * (b - a); }

		public static float2 Lerp(float2 a, float2 b, float t)
		{
			return new float2(
				Lerp(a.X, b.X, t),
				Lerp(a.Y, b.Y, t));
		}

		public static float2 Lerp(float2 a, float2 b, float2 t)
		{
			return new float2(
				Lerp(a.X, b.X, t.X),
				Lerp(a.Y, b.Y, t.Y));
		}

		public static float2 FromAngle(float a) { return new float2((float)Math.Sin(a), (float)Math.Cos(a)); }

		static float Constrain(float x, float a, float b) { return x < a ? a : x > b ? b : x; }

		public float2 Constrain(float2 min, float2 max)
		{
			return new float2(
				Constrain(X, min.X, max.X),
				Constrain(Y, min.Y, max.Y));
		}

		public static float2 operator *(float a, float2 b) { return new float2(a * b.X, a * b.Y); }
		public static float2 operator *(float2 b, float a) { return new float2(a * b.X, a * b.Y); }
		public static float2 operator *(float2 a, float2 b) { return new float2(a.X * b.X, a.Y * b.Y); }
		public static float2 operator /(float2 a, float2 b) { return new float2(a.X / b.X, a.Y / b.Y); }
		public static float2 operator /(float2 a, float b) { return new float2(a.X / b, a.Y / b); }

		public static bool operator ==(float2 me, float2 other) { return me.X == other.X && me.Y == other.Y; }
		public static bool operator !=(float2 me, float2 other) { return !(me == other); }

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public bool Equals(float2 other) { return this == other; }
		public override bool Equals(object obj) { return obj is float2 && Equals((float2)obj); }

		public override string ToString() { return X + "," + Y; }

		public static readonly float2 Zero = new float2(0, 0);

		public static bool WithinEpsilon(float2 a, float2 b, float e)
		{
			var d = a - b;
			return Math.Abs(d.X) < e && Math.Abs(d.Y) < e;
		}

		public float2 Sign() { return new float2(Math.Sign(X), Math.Sign(Y)); }
		public static float Dot(float2 a, float2 b) { return a.X * b.X + a.Y * b.Y; }
		public float2 Round() { return new float2((float)Math.Round(X), (float)Math.Round(Y)); }

		public int2 ToInt2() { return new int2((int)X, (int)Y); }

		public static float2 Max(float2 a, float2 b) { return new float2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static float2 Min(float2 a, float2 b) { return new float2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public float LengthSquared => X * X + Y * Y;
		public float Length => (float)Math.Sqrt(LengthSquared);
	}

	public class EWMA
	{
		readonly float animRate;
		float? value;

		public EWMA(float animRate) { this.animRate = animRate; }

		public float Update(float newValue)
		{
			value = float2.Lerp(value ?? newValue, newValue, animRate);
			return value.Value;
		}
	}
}
