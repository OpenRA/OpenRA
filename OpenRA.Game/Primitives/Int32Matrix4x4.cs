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

namespace OpenRA
{
	public readonly struct Int32Matrix4x4 : IEquatable<Int32Matrix4x4>
	{
		public readonly int M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44;

		public Int32Matrix4x4(
			int m11, int m12, int m13, int m14,
			int m21, int m22, int m23, int m24,
			int m31, int m32, int m33, int m34,
			int m41, int m42, int m43, int m44)
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

		public static bool operator ==(Int32Matrix4x4 me, Int32Matrix4x4 other)
		{
			return
				me.M11 == other.M11 && me.M12 == other.M12 && me.M13 == other.M13 && me.M14 == other.M14 &&
				me.M21 == other.M21 && me.M22 == other.M22 && me.M23 == other.M23 && me.M24 == other.M24 &&
				me.M31 == other.M31 && me.M32 == other.M32 && me.M33 == other.M33 && me.M34 == other.M34 &&
				me.M41 == other.M41 && me.M42 == other.M42 && me.M43 == other.M43 && me.M44 == other.M44;
		}

		public static bool operator !=(Int32Matrix4x4 me, Int32Matrix4x4 other) { return !(me == other); }

		public override int GetHashCode() { return M11 ^ M22 ^ M33 ^ M44; }

		public bool Equals(Int32Matrix4x4 other) { return other == this; }
		public override bool Equals(object obj) { return obj is Int32Matrix4x4 && Equals((Int32Matrix4x4)obj); }

		public override string ToString()
		{
			return
				"[" + M11 + " " + M12 + " " + M13 + " " + M14 + "],[" +
				"[" + M21 + " " + M22 + " " + M23 + " " + M24 + "],[" +
				"[" + M31 + " " + M32 + " " + M33 + " " + M34 + "],[" +
				"[" + M41 + " " + M42 + " " + M43 + " " + M44 + "]";
		}
	}
}
