#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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

		public int[] AsQuarternion()
		{
			// Angles increase clockwise
			var r = new WAngle(-Roll.Angle / 2);
			var p = new WAngle(-Pitch.Angle / 2);
			var y = new WAngle(-Yaw.Angle / 2);
			var cr = (long)r.Cos();
			var sr = (long)r.Sin();
			var cp = (long)p.Cos();
			var sp = (long)p.Sin();
			var cy = (long)y.Cos();
			var sy = (long)y.Sin();

			// Normalized to 1024 == 1.0
			return new int[4]
			{
				(int)((sr * cp * cy - cr * sp * sy) / 1048576), // x
				(int)((cr * sp * cy + sr * cp * sy) / 1048576), // y
				(int)((cr * cp * sy - sr * sp * cy) / 1048576), // z
				(int)((cr * cp * cy + sr * sp * sy) / 1048576)  // w
			};
		}

		public int[] AsMatrix()
		{
			var q = AsQuarternion();

			// Theoretically 1024 *  * 2, but may differ slightly due to rounding
			var lsq = q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3];

			// Quaternion components use 10 bits, so there's no risk of overflow
			var mtx = new int[16];
			mtx[0] = lsq - 2 * (q[1] * q[1] + q[2] * q[2]);
			mtx[1] = 2 * (q[0] * q[1] + q[2] * q[3]);
			mtx[2] = 2 * (q[0] * q[2] - q[1] * q[3]);
			mtx[3] = 0;

			mtx[4] = 2 * (q[0] * q[1] - q[2] * q[3]);
			mtx[5] = lsq - 2 * (q[0] * q[0] + q[2] * q[2]);
			mtx[6] = 2 * (q[1] * q[2] + q[0] * q[3]);
			mtx[7] = 0;

			mtx[8] = 2 * (q[0] * q[2] + q[1] * q[3]);
			mtx[9] = 2 * (q[1] * q[2] - q[0] * q[3]);
			mtx[10] = lsq - 2 * (q[0] * q[0] + q[1] * q[1]);
			mtx[11] = 0;

			mtx[12] = 0;
			mtx[13] = 0;
			mtx[14] = 0;
			mtx[15] = lsq;

			return mtx;
		}

		public override int GetHashCode() { return Roll.GetHashCode() ^ Pitch.GetHashCode() ^ Yaw.GetHashCode(); }

		public bool Equals(WRot other) { return other == this; }
		public override bool Equals(object obj) { return obj is WRot && Equals((WRot)obj); }

		public override string ToString() { return Roll + "," + Pitch + "," + Yaw; }
	}
}
