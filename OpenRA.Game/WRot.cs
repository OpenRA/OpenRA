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
	/// <summary>
	/// 3d World rotation.
	/// </summary>
	public struct WRot : IEquatable<WRot>
	{
		public readonly WAngle Roll, Pitch, Yaw;

		public WRot(WAngle roll, WAngle pitch, WAngle yaw) { Roll = roll; Pitch = pitch; Yaw = yaw; }
		public static readonly WRot Zero = new WRot(WAngle.Zero, WAngle.Zero, WAngle.Zero);

		public static WRot FromFacing(int facing) { return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing)); }
		public static WRot FromYaw(WAngle yaw) { return new WRot(WAngle.Zero, WAngle.Zero, yaw); }
		public static WRot operator +(WRot a, WRot b) { return new WRot(a.Roll + b.Roll, a.Pitch + b.Pitch, a.Yaw + b.Yaw); }
		public static WRot operator -(WRot a, WRot b) { return new WRot(a.Roll - b.Roll, a.Pitch - b.Pitch, a.Yaw - b.Yaw); }
		public static WRot operator -(WRot a) { return new WRot(-a.Roll, -a.Pitch, -a.Yaw); }

		public static bool operator ==(WRot me, WRot other)
		{
			return me.Roll == other.Roll && me.Pitch == other.Pitch && me.Yaw == other.Yaw;
		}

		public static bool operator !=(WRot me, WRot other) { return !(me == other); }

		public WRot WithYaw(WAngle yaw)
		{
			return new WRot(Roll, Pitch, yaw);
		}

		void AsQuarternion(out int x, out int y, out int z, out int w)
		{
			// Angles increase clockwise
			var roll = new WAngle(-Roll.Angle / 2);
			var pitch = new WAngle(-Pitch.Angle / 2);
			var yaw = new WAngle(-Yaw.Angle / 2);
			var cr = (long)roll.Cos();
			var sr = (long)roll.Sin();
			var cp = (long)pitch.Cos();
			var sp = (long)pitch.Sin();
			var cy = (long)yaw.Cos();
			var sy = (long)yaw.Sin();

			// Normalized to 1024 == 1.0
			x = (int)((sr * cp * cy - cr * sp * sy) / 1048576);
			y = (int)((cr * sp * cy + sr * cp * sy) / 1048576);
			z = (int)((cr * cp * sy - sr * sp * cy) / 1048576);
			w = (int)((cr * cp * cy + sr * sp * sy) / 1048576);
		}

		public void AsMatrix(out Int32Matrix4x4 mtx)
		{
			int x, y, z, w;
			AsQuarternion(out x, out y, out z, out w);

			// Theoretically 1024 *  * 2, but may differ slightly due to rounding
			var lsq = x * x + y * y + z * z + w * w;

			// Quaternion components use 10 bits, so there's no risk of overflow
			#pragma warning disable SA1115 // Allow blank lines to visually separate matrix rows
			mtx = new Int32Matrix4x4(
				lsq - 2 * (y * y + z * z),
				2 * (x * y + z * w),
				2 * (x * z - y * w),
				0,

				2 * (x * y - z * w),
				lsq - 2 * (x * x + z * z),
				2 * (y * z + x * w),
				0,

				2 * (x * z + y * w),
				2 * (y * z - x * w),
				lsq - 2 * (x * x + y * y),
				0,

				0,
				0,
				0,
				lsq);
			#pragma warning restore SA1115
		}

		public Int32Matrix4x4 AsMatrix()
		{
			Int32Matrix4x4 mtx;
			AsMatrix(out mtx);
			return mtx;
		}

		public override int GetHashCode() { return Roll.GetHashCode() ^ Pitch.GetHashCode() ^ Yaw.GetHashCode(); }

		public bool Equals(WRot other) { return other == this; }
		public override bool Equals(object obj) { return obj is WRot && Equals((WRot)obj); }

		public override string ToString() { return Roll + "," + Pitch + "," + Yaw; }
	}
}
