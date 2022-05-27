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

namespace OpenRA
{
	/// <summary>
	/// 3d World rotation.
	/// </summary>
	public readonly struct WRot : IEquatable<WRot>
	{
		// The Euler angle representation is a lot more intuitive for public use
		public readonly WAngle Roll, Pitch, Yaw;

		// Internal calculations use the quaternion form
		readonly int x, y, z, w;

		/// <summary>
		/// Construct a rotation from euler angles.
		/// </summary>
		public WRot(WAngle roll, WAngle pitch, WAngle yaw)
		{
			Roll = roll;
			Pitch = pitch;
			Yaw = yaw;

			// Angles increase clockwise
			var qr = new WAngle(-Roll.Angle / 2);
			var qp = new WAngle(-Pitch.Angle / 2);
			var qy = new WAngle(-Yaw.Angle / 2);
			var cr = (long)qr.Cos();
			var sr = (long)qr.Sin();
			var cp = (long)qp.Cos();
			var sp = (long)qp.Sin();
			var cy = (long)qy.Cos();
			var sy = (long)qy.Sin();

			// Normalized to 1024 == 1.0
			x = (int)((sr * cp * cy - cr * sp * sy) / 1048576);
			y = (int)((cr * sp * cy + sr * cp * sy) / 1048576);
			z = (int)((cr * cp * sy - sr * sp * cy) / 1048576);
			w = (int)((cr * cp * cy + sr * sp * sy) / 1048576);
		}

		/// <summary>
		/// Construct a rotation from an axis and angle.
		/// The axis is expected to be normalized to length 1024
		/// </summary>
		public WRot(WVec axis, WAngle angle)
		{
			// Angles increase clockwise
			x = axis.X * new WAngle(-angle.Angle / 2).Sin() / 1024;
			y = axis.Y * new WAngle(-angle.Angle / 2).Sin() / 1024;
			z = axis.Z * new WAngle(-angle.Angle / 2).Sin() / 1024;
			w = new WAngle(-angle.Angle / 2).Cos();

			(Roll, Pitch, Yaw) = QuaternionToEuler(x, y, z, w);
		}

		WRot(int x, int y, int z, int w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;

			(Roll, Pitch, Yaw) = QuaternionToEuler(x, y, z, w);
		}

		static (WAngle, WAngle, WAngle) QuaternionToEuler(int x, int y, int z, int w)
		{
			// Theoretically 1024 squared, but may differ slightly due to rounding
			var lsq = x * x + y * y + z * z + w * w;

			var srcp = 2 * (w * x + y * z);
			var crcp = lsq - 2 * (x * x + y * y);
			var sp = (w * y - z * x) / 512;
			var sycp = 2 * (w * z + x * y);
			var cycp = lsq - 2 * (y * y + z * z);

			var roll = -WAngle.ArcTan(srcp, crcp);
			var pitch = -(Math.Abs(sp) >= 1024 ? new WAngle(Math.Sign(sp) * 256) : WAngle.ArcSin(sp));
			var yaw = -WAngle.ArcTan(sycp, cycp);

			return (roll, pitch, yaw);
		}

		WRot(int x, int y, int z, int w, WAngle roll, WAngle pitch, WAngle yaw)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
			Roll = roll;
			Pitch = pitch;
			Yaw = yaw;
		}

		public static readonly WRot None = new WRot(WAngle.Zero, WAngle.Zero, WAngle.Zero);

		public static WRot FromFacing(int facing) { return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing)); }
		public static WRot FromYaw(WAngle yaw) { return new WRot(WAngle.Zero, WAngle.Zero, yaw); }
		public static WRot operator +(in WRot a, in WRot b) { return new WRot(a.Roll + b.Roll, a.Pitch + b.Pitch, a.Yaw + b.Yaw); }
		public static WRot operator -(in WRot a, in WRot b) { return new WRot(a.Roll - b.Roll, a.Pitch - b.Pitch, a.Yaw - b.Yaw); }
		public static WRot operator -(in WRot a) { return new WRot(-a.x, -a.y, -a.z, a.w, -a.Roll, -a.Pitch, -a.Yaw); }

		public WRot Rotate(in WRot rot)
		{
			if (this == None)
				return rot;

			if (rot == None)
				return this;

			var rx = ((long)rot.w * x + (long)rot.x * w + (long)rot.y * z - (long)rot.z * y) / 1024;
			var ry = ((long)rot.w * y - (long)rot.x * z + (long)rot.y * w + (long)rot.z * x) / 1024;
			var rz = ((long)rot.w * z + (long)rot.x * y - (long)rot.y * x + (long)rot.z * w) / 1024;
			var rw = ((long)rot.w * w - (long)rot.x * x - (long)rot.y * y - (long)rot.z * z) / 1024;

			return new WRot((int)rx, (int)ry, (int)rz, (int)rw);
		}

		public static bool operator ==(in WRot me, in WRot other)
		{
			return me.Roll == other.Roll && me.Pitch == other.Pitch && me.Yaw == other.Yaw;
		}

		public static bool operator !=(in WRot me, in WRot other) { return !(me == other); }

		public WRot WithRoll(WAngle roll)
		{
			return new WRot(roll, Pitch, Yaw);
		}

		public WRot WithPitch(WAngle pitch)
		{
			return new WRot(Roll, pitch, Yaw);
		}

		public WRot WithYaw(WAngle yaw)
		{
			return new WRot(Roll, Pitch, yaw);
		}

		public void AsMatrix(out Int32Matrix4x4 mtx)
		{
			// Theoretically 1024 squared, but may differ slightly due to rounding
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
			AsMatrix(out var mtx);
			return mtx;
		}

		public override int GetHashCode() { return Roll.GetHashCode() ^ Pitch.GetHashCode() ^ Yaw.GetHashCode(); }

		public bool Equals(WRot other) { return other == this; }
		public override bool Equals(object obj) { return obj is WRot && Equals((WRot)obj); }

		public override string ToString() { return Roll + "," + Pitch + "," + Yaw; }

		public static WRot SLerp(in WRot a, in WRot b, int mul, int div)
		{
			// This implements the standard spherical linear interpolation
			// between two quaternions, accounting for OpenRA's integer math
			// conventions and WRot always using (nearly) normalized quaternions
			var dot = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
			var flip = dot >= 0 ? 1 : -1;

			// a and b describe the same rotation
			if (flip * dot >= 1024 * 1024)
				return a;

			var theta = WAngle.ArcCos(dot / 1024);
			var s1 = new WAngle((div - mul) * theta.Angle / div).Sin();
			var s2 = new WAngle(mul * theta.Angle / div).Sin();
			var s3 = theta.Sin();

			var x = ((long)a.x * s1 + flip * b.x * s2) / s3;
			var y = ((long)a.y * s1 + flip * b.y * s2) / s3;
			var z = ((long)a.z * s1 + flip * b.z * s2) / s3;
			var w = ((long)a.w * s1 + flip * b.w * s2) / s3;

			// Normalize to 1024 == 1.0
			var l = Exts.ISqrt(x * x + y * y + z * z + w * w);
			return new WRot((int)(1024 * x / l), (int)(1024 * y / l), (int)(1024 * z / l), (int)(1024 * w / l));
		}
	}
}
