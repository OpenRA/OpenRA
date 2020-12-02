#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA
{
	[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Mimic a built-in type alias.")]
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct float3 : IEquatable<float3>
	{
		public readonly float X, Y, Z;
		public float2 XY { get { return new float2(X, Y); } }

		public float3(float x, float y, float z) { X = x; Y = y; Z = z; }
		public float3(float2 xy, float z) { X = xy.X; Y = xy.Y; Z = z; }

		public static implicit operator float3(int2 src) { return new float3(src.X, src.Y, 0); }
		public static implicit operator float3(float2 src) { return new float3(src.X, src.Y, 0); }

		public static float3 operator +(in float3 a, in float3 b) { return new float3(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
		public static float3 operator -(in float3 a, in float3 b) { return new float3(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
		public static float3 operator -(in float3 a) { return new float3(-a.X, -a.Y, -a.Z); }
		public static float3 operator *(in float3 a, in float3 b) { return new float3(a.X * b.X, a.Y * b.Y, a.Z * b.Z); }
		public static float3 operator *(float a, in float3 b) { return new float3(a * b.X, a * b.Y, a * b.Z); }
		public static float3 operator /(in float3 a, in float3 b) { return new float3(a.X / b.X, a.Y / b.Y, a.Z / b.Z); }
		public static float3 operator /(in float3 a, float b) { return new float3(a.X / b, a.Y / b, a.Z / b); }

		public static float3 Lerp(float3 a, float3 b, float t)
		{
			return new float3(
				float2.Lerp(a.X, b.X, t),
				float2.Lerp(a.Y, b.Y, t),
				float2.Lerp(a.Z, b.Z, t));
		}

		public static bool operator ==(in float3 me, in float3 other) { return me.X == other.X && me.Y == other.Y && me.Z == other.Z; }
		public static bool operator !=(in float3 me, in float3 other) { return !(me == other); }
		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode(); }

		public bool Equals(float3 other)
		{
			return other == this;
		}

		public override bool Equals(object obj)
		{
			var o = obj as float3?;
			return o != null && o == this;
		}

		public override string ToString() { return "{0},{1},{2}".F(X, Y, Z); }

		public static readonly float3 Zero = new float3(0, 0, 0);
		public static readonly float3 Ones = new float3(1, 1, 1);
	}
}
