#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Mimic a built-in type alias.")]
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct Float3 : IEquatable<Float3>
	{
		public readonly float X, Y, Z;
		public float2 XY => new float2(X, Y);

		public Float3(float x, float y, float z) { X = x; Y = y; Z = z; }
		public Float3(float2 xy, float z) { X = xy.X; Y = xy.Y; Z = z; }

		public static implicit operator Float3(Int2 src) { return new Float3(src.X, src.Y, 0); }
		public static implicit operator Float3(float2 src) { return new Float3(src.X, src.Y, 0); }

		public static Float3 operator +(in Float3 a, in Float3 b) { return new Float3(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
		public static Float3 operator -(in Float3 a, in Float3 b) { return new Float3(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
		public static Float3 operator -(in Float3 a) { return new Float3(-a.X, -a.Y, -a.Z); }
		public static Float3 operator *(in Float3 a, in Float3 b) { return new Float3(a.X * b.X, a.Y * b.Y, a.Z * b.Z); }
		public static Float3 operator *(float a, in Float3 b) { return new Float3(a * b.X, a * b.Y, a * b.Z); }
		public static Float3 operator /(in Float3 a, in Float3 b) { return new Float3(a.X / b.X, a.Y / b.Y, a.Z / b.Z); }
		public static Float3 operator /(in Float3 a, float b) { return new Float3(a.X / b, a.Y / b, a.Z / b); }

		public static Float3 Lerp(Float3 a, Float3 b, float t)
		{
			return new Float3(
				float2.Lerp(a.X, b.X, t),
				float2.Lerp(a.Y, b.Y, t),
				float2.Lerp(a.Z, b.Z, t));
		}

		public static bool operator ==(in Float3 me, in Float3 other) { return me.X == other.X && me.Y == other.Y && me.Z == other.Z; }
		public static bool operator !=(in Float3 me, in Float3 other) { return !(me == other); }
		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode(); }

		public bool Equals(Float3 other)
		{
			return other == this;
		}

		public override bool Equals(object obj)
		{
			return obj is Float3 o && (Float3?)o == this;
		}

		public override string ToString() { return $"{X},{Y},{Z}"; }

		public static readonly Float3 Zero = new Float3(0, 0, 0);
		public static readonly Float3 Ones = new Float3(1, 1, 1);
	}
}
