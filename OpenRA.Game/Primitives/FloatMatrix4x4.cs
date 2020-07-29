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

namespace OpenRA
{
	public struct FloatMatrix4x4 : IEquatable<FloatMatrix4x4>
	{
		public static readonly FloatMatrix4x4 Identity = new FloatMatrix4x4(
			1, 0, 0, 0,
			0, 1, 0, 0,
			0, 0, 1, 0,
			0, 0, 0, 1);

		public readonly float M11, M12, M13, M14;
		public readonly float M21, M22, M23, M24;
		public readonly float M31, M32, M33, M34;
		public readonly float M41, M42, M43, M44;

		public FloatMatrix4x4(
			float m11, float m12, float m13, float m14,
			float m21, float m22, float m23, float m24,
			float m31, float m32, float m33, float m34,
			float m41, float m42, float m43, float m44)
		{
			M11 = m11;
			M12 = m12;
			M13 = m13;
			M14 = m14;

			M21 = m21;
			M22 = m22;
			M23 = m23;
			M24 = m24;

			M31 = m31;
			M32 = m32;
			M33 = m33;
			M34 = m34;

			M41 = m41;
			M42 = m42;
			M43 = m43;
			M44 = m44;
		}

		public FloatMatrix4x4 Invert()
		{
			var mtx = new FloatMatrix4x4(
				M22 * M33 * M44 - M22 * M34 * M43 - M32 * M23 * M44 + M32 * M24 * M43 + M42 * M23 * M34 - M42 * M24 * M33,
				-M12 * M33 * M44 + M12 * M34 * M43 + M32 * M13 * M44 - M32 * M14 * M43 - M42 * M13 * M34 + M42 * M14 * M33,
				M12 * M23 * M44 - M12 * M24 * M43 - M22 * M13 * M44 + M22 * M14 * M43 + M42 * M13 * M24 - M42 * M14 * M23,
				-M12 * M23 * M34 + M12 * M24 * M33 + M22 * M13 * M34 - M22 * M14 * M33 - M32 * M13 * M24 + M32 * M14 * M23,
				-M21 * M33 * M44 + M21 * M34 * M43 + M31 * M23 * M44 - M31 * M24 * M43 - M41 * M23 * M34 + M41 * M24 * M33,
				M11 * M33 * M44 - M11 * M34 * M43 - M31 * M13 * M44 + M31 * M14 * M43 + M41 * M13 * M34 - M41 * M14 * M33,
				-M11 * M23 * M44 + M11 * M24 * M43 + M21 * M13 * M44 - M21 * M14 * M43 - M41 * M13 * M24 + M41 * M14 * M23,
				M11 * M23 * M34 - M11 * M24 * M33 - M21 * M13 * M34 + M21 * M14 * M33 + M31 * M13 * M24 - M31 * M14 * M23,
				M21 * M32 * M44 - M21 * M34 * M42 - M31 * M22 * M44 + M31 * M24 * M42 + M41 * M22 * M34 - M41 * M24 * M32,
				-M11 * M32 * M44 + M11 * M34 * M42 + M31 * M12 * M44 - M31 * M14 * M42 - M41 * M12 * M34 + M41 * M14 * M32,
				M11 * M22 * M44 - M11 * M24 * M42 - M21 * M12 * M44 + M21 * M14 * M42 + M41 * M12 * M24 - M41 * M14 * M22,
				-M11 * M22 * M34 + M11 * M24 * M32 + M21 * M12 * M34 - M21 * M14 * M32 - M31 * M12 * M24 + M31 * M14 * M22,
				-M21 * M32 * M43 + M21 * M33 * M42 + M31 * M22 * M43 - M31 * M23 * M42 - M41 * M22 * M33 + M41 * M23 * M32,
				M11 * M32 * M43 - M11 * M33 * M42 - M31 * M12 * M43 + M31 * M13 * M42 + M41 * M12 * M33 - M41 * M13 * M32,
				-M11 * M22 * M43 + M11 * M23 * M42 + M21 * M12 * M43 - M21 * M13 * M42 - M41 * M12 * M23 + M41 * M13 * M22,
				M11 * M22 * M33 - M11 * M23 * M32 - M21 * M12 * M33 + M21 * M13 * M32 + M31 * M12 * M23 - M31 * M13 * M22);

			var det = M11 * mtx.M11 + M12 * mtx.M21 + M13 * mtx.M31 + M14 * mtx.M41;
			if (det == 0)
				throw new InvalidOperationException("Matrix cannot be inverted.");

			return new FloatMatrix4x4(
				mtx.M11 / det, mtx.M12 / det, mtx.M13 / det, mtx.M14 / det,
				mtx.M21 / det, mtx.M22 / det, mtx.M23 / det, mtx.M24 / det,
				mtx.M31 / det, mtx.M32 / det, mtx.M33 / det, mtx.M34 / det,
				mtx.M41 / det, mtx.M42 / det, mtx.M43 / det, mtx.M44 / det);
		}

		public float[] Unpack()
		{
			return new[]
			{
				M11, M12, M13, M14,
				M21, M22, M23, M24,
				M31, M32, M33, M34,
				M41, M42, M43, M44,
			};
		}

		public static FloatMatrix4x4 CreateScale(float3 scale)
		{
			return new FloatMatrix4x4(
				scale.X, Identity.M12, Identity.M13, Identity.M14,
				Identity.M21, scale.Y, Identity.M23, Identity.M24,
				Identity.M31, Identity.M32, scale.Z, Identity.M34,
				Identity.M41, Identity.M42, Identity.M43, Identity.M44);
		}

		public static FloatMatrix4x4 CreateTranslation(float3 translation)
		{
			return new FloatMatrix4x4(
				Identity.M11, Identity.M12, Identity.M13, Identity.M14,
				Identity.M21, Identity.M22, Identity.M23, Identity.M24,
				Identity.M31, Identity.M32, Identity.M33, Identity.M34,
				translation.X, translation.Y, translation.Z, Identity.M44);
		}

		public static FloatMatrix4x4 operator *(FloatMatrix4x4 a, FloatMatrix4x4 b)
		{
			return new FloatMatrix4x4(
				a.M11 * b.M11 + a.M21 * b.M12 + a.M31 * b.M13 + a.M41 * b.M14,
				a.M12 * b.M11 + a.M22 * b.M12 + a.M32 * b.M13 + a.M42 * b.M14,
				a.M13 * b.M11 + a.M23 * b.M12 + a.M33 * b.M13 + a.M43 * b.M14,
				a.M14 * b.M11 + a.M24 * b.M12 + a.M34 * b.M13 + a.M44 * b.M14,
				a.M11 * b.M21 + a.M21 * b.M22 + a.M31 * b.M23 + a.M41 * b.M24,
				a.M12 * b.M21 + a.M22 * b.M22 + a.M32 * b.M23 + a.M42 * b.M24,
				a.M13 * b.M21 + a.M23 * b.M22 + a.M33 * b.M23 + a.M43 * b.M24,
				a.M14 * b.M21 + a.M24 * b.M22 + a.M34 * b.M23 + a.M44 * b.M24,
				a.M11 * b.M31 + a.M21 * b.M32 + a.M31 * b.M33 + a.M41 * b.M34,
				a.M12 * b.M31 + a.M22 * b.M32 + a.M32 * b.M33 + a.M42 * b.M34,
				a.M13 * b.M31 + a.M23 * b.M32 + a.M33 * b.M33 + a.M43 * b.M34,
				a.M14 * b.M31 + a.M24 * b.M32 + a.M34 * b.M33 + a.M44 * b.M34,
				a.M11 * b.M41 + a.M21 * b.M42 + a.M31 * b.M43 + a.M41 * b.M44,
				a.M12 * b.M41 + a.M22 * b.M42 + a.M32 * b.M43 + a.M42 * b.M44,
				a.M13 * b.M41 + a.M23 * b.M42 + a.M33 * b.M43 + a.M43 * b.M44,
				a.M14 * b.M41 + a.M24 * b.M42 + a.M34 * b.M43 + a.M44 * b.M44);
		}

		public static float4 operator *(FloatMatrix4x4 a, float4 b)
		{
			return new float4(
				a.M11 * b.X + a.M21 * b.Y + a.M31 * b.Z + a.M41 * b.W,
				a.M12 * b.X + a.M22 * b.Y + a.M32 * b.Z + a.M42 * b.W,
				a.M13 * b.X + a.M23 * b.Y + a.M33 * b.Z + a.M43 * b.W,
				a.M14 * b.X + a.M24 * b.Y + a.M34 * b.Z + a.M44 * b.W);
		}

		public static bool operator ==(FloatMatrix4x4 me, FloatMatrix4x4 other)
		{
			return
				me.M11 == other.M11 && me.M12 == other.M12 && me.M13 == other.M13 && me.M14 == other.M14 &&
				me.M21 == other.M21 && me.M22 == other.M22 && me.M23 == other.M23 && me.M24 == other.M24 &&
				me.M31 == other.M31 && me.M32 == other.M32 && me.M33 == other.M33 && me.M34 == other.M34 &&
				me.M41 == other.M41 && me.M42 == other.M42 && me.M43 == other.M43 && me.M44 == other.M44;
		}

		public static bool operator !=(FloatMatrix4x4 me, FloatMatrix4x4 other) { return !(me == other); }

		public override int GetHashCode() { return M11.GetHashCode() ^ M22.GetHashCode() ^ M33.GetHashCode() ^ M44.GetHashCode(); }

		public bool Equals(FloatMatrix4x4 other) { return other == this; }
		public override bool Equals(object obj) { return obj is FloatMatrix4x4 && Equals((FloatMatrix4x4)obj); }

		public override string ToString()
		{
			return
				"[" + M11 + " " + M12 + " " + M13 + " " + M14 + "]," +
				"[" + M21 + " " + M22 + " " + M23 + " " + M24 + "]," +
				"[" + M31 + " " + M32 + " " + M33 + " " + M34 + "]," +
				"[" + M41 + " " + M42 + " " + M43 + " " + M44 + "]";
		}
	}
}
