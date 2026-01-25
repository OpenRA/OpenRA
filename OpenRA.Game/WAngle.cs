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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Scripting;

namespace OpenRA
{
	/// <summary>
	/// 1D angle - 1024 units = 360 degrees.
	/// </summary>
	public readonly struct WAngle : IEquatable<WAngle>, IScriptBindable,
		ILuaAdditionBinding, ILuaSubtractionBinding, ILuaEqualityBinding, ILuaTableBinding, ILuaToStringBinding
	{
		public readonly int Angle;
		public int AngleSquared => Angle * Angle;

		public WAngle(int a)
		{
			// Bitwise mask handles wrapping and negatives.
			Angle = a & 1023;
		}

		public static readonly WAngle Zero = new(0);
		public static WAngle FromFacing(int facing) { return new WAngle(facing * 4); }
		public static WAngle FromDegrees(int degrees) { return new WAngle(degrees * 1024 / 360); }
		public static WAngle operator +(WAngle a, WAngle b) { return new WAngle(a.Angle + b.Angle); }
		public static WAngle operator -(WAngle a, WAngle b) { return new WAngle(a.Angle - b.Angle); }
		public static WAngle operator -(WAngle a) { return new WAngle(-a.Angle); }

		public static WAngle operator *(WAngle a, int b) { return new WAngle(a.Angle * b); }
		public static WAngle operator *(int a, WAngle b) { return new WAngle(a * b.Angle); }
		public static WAngle operator /(WAngle a, int b) { return new WAngle(a.Angle / b); }
		public static int operator /(WAngle a, WAngle b) { return a.Angle / b.Angle; }

		public static bool operator ==(WAngle me, WAngle other) { return me.Angle == other.Angle; }
		public static bool operator !=(WAngle me, WAngle other) { return me.Angle != other.Angle; }

		public override int GetHashCode() { return Angle.GetHashCode(); }

		public bool Equals(WAngle other) { return other.Angle == Angle; }
		public override bool Equals(object obj) { return obj is WAngle angle && Angle == angle.Angle; }

		public int Facing => Angle / 4;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Sin()
		{
			return new WAngle(Angle - 256).Cos();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Cos()
		{
			// Use symmetry to map the 1024-unit circle into a 0-256 quadrant index.
			// Cosine is negative in the Q2 and Q3.
			var angle = Angle;

			// Maps 0-511 into a 0-256 mirrored triangle wave.
			var qIndex = angle & 511;
			var mirrored = 256 - qIndex;

			// Branchless Abs(256 - qIndex).
			var mask = mirrored >> 31;
			var finalIndex = 256 - ((mirrored ^ mask) - mask);

			// Phase-shift by 90 degrees (256-units) to align negative hemisphere with 9th bit.
			// Maps bit value 0 -> 1 and 1 -> -1 branchlessly.
			var signBit = (int)((uint)(angle + 256) >> 9) & 1;
			var sign = 1 - (signBit << 1);

			ref var table = ref MemoryMarshal.GetArrayDataReference(CosineTable);
			return sign * Unsafe.Add(ref table, (nint)(uint)finalIndex);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Tan()
		{
			var angle = Angle & 511;

			// Shift by 257 to keep the 90 degree asymptote (256) positive.
			var shifted = angle - 257;
			var mask = shifted >> 31;

			// Branchless Abs(angle - 256) to map 0-511 range to a 256-0-255 triangle.
			var triangle = ((angle - 256) ^ (angle - 256 >> 31)) - (angle - 256 >> 31);
			var finalIndex = 256 - triangle;

			// Maps bit value -1 -> 1 and 0 -> -1 branchlessly.
			var sign = -1 - (mask << 1);

			ref var table = ref MemoryMarshal.GetArrayDataReference(TanTable);
			return sign * Unsafe.Add(ref table, (nint)(uint)finalIndex);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static WAngle Lerp(WAngle a, WAngle b, int mul, int div)
		{
			// Linearize endpoints by shifting across the 1024-unit wrap if it yields a shorter path.
			var start = a.Angle;
			var diff = b.Angle - start;

			// Shift difference to take shortest path around the circle.
			var mask1 = (511 - diff) >> 31;
			var mask2 = (diff + 512) >> 31;
			diff += (mask1 & -1024) | (mask2 & 1024);

			return new WAngle(start + diff * mul / div);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static WAngle ArcSin(int d)
		{
			// Range check using unsigned trick
			if ((uint)(d + 1024) > 2048)
				ThrowOutOfRange();

			var index = GetClosestCosineIndex(Math.Abs(d));
			var sign = d >> 31;

			// Map positive to Q1 (0-256) and negative to Q4 (768-1024).
			return new WAngle((sign & (768 + index)) | (~sign & (256 - index)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static WAngle ArcCos(int d)
		{
			if ((uint)(d + 1024) > 2048)
				ThrowOutOfRange();

			var index = GetClosestCosineIndex(Math.Abs(d));
			var sign = d >> 31;

			return new WAngle((sign & (512 - index)) | (~sign & index));
		}

		static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int GetClosestCosineIndex(int value)
		{
			ref var table = ref MemoryMarshal.GetArrayDataReference(CosineTable);
			var index = 0;

			// Waterfall binary search finds the lower bound; this picks the closest neighbor.
			index |= (Unsafe.Add(ref table, index | 128) > value) ? 128 : 0;
			index |= (Unsafe.Add(ref table, index | 64) > value) ? 64 : 0;
			index |= (Unsafe.Add(ref table, index | 32) > value) ? 32 : 0;
			index |= (Unsafe.Add(ref table, index | 16) > value) ? 16 : 0;
			index |= (Unsafe.Add(ref table, index | 8) > value) ? 8 : 0;
			index |= (Unsafe.Add(ref table, index | 4) > value) ? 4 : 0;
			index |= (Unsafe.Add(ref table, index | 2) > value) ? 2 : 0;
			index |= (Unsafe.Add(ref table, index | 1) > value) ? 1 : 0;

			int val0 = Unsafe.Add(ref table, index);
			int val1 = Unsafe.Add(ref table, Math.Min(index + 1, 256));

			// Pick the one with the smallest absolute difference.
			return (val0 - value > value - val1) ? index + 1 : index;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static WAngle ArcTan(int y, int x)
		{
			if (y == 0)
				return new WAngle(x >= 0 ? 0 : 512);

			if (x == 0)
				return new WAngle(y > 0 ? 256 : 768);

			var ay = Math.Abs((long)y);
			var ax = Math.Abs((long)x);

			// Return 90 degrees if the ratio exceeds the tangent table's precision limit (~89.6 deg).
			if (ay >= ax * 167)
				return new WAngle(y > 0 ? 256 : 768);

			// Use a bitshift and single division to find the target ratio.
			// This allows the binary search to compare simple integers instead of performing multiplications.
			var target = (int)((ay << 10) / ax);

			ref var table = ref MemoryMarshal.GetArrayDataReference(TanTable);
			var index = 0;

			// Waterfall binary search avoids loop overhead and branch mispredicts.
			index |= (Unsafe.Add(ref table, index | 128) <= target) ? 128 : 0;
			index |= (Unsafe.Add(ref table, index | 64) <= target) ? 64 : 0;
			index |= (Unsafe.Add(ref table, index | 32) <= target) ? 32 : 0;
			index |= (Unsafe.Add(ref table, index | 16) <= target) ? 16 : 0;
			index |= (Unsafe.Add(ref table, index | 8) <= target) ? 8 : 0;
			index |= (Unsafe.Add(ref table, index | 4) <= target) ? 4 : 0;
			index |= (Unsafe.Add(ref table, index | 2) <= target) ? 2 : 0;
			index |= (Unsafe.Add(ref table, index | 1) <= target) ? 1 : 0;

			var val = Unsafe.Add(ref table, index);
			var nextVal = Unsafe.Add(ref table, index + 1);
			index += (target - val > nextVal - target) ? 1 : 0;

			// Map the reference angle into the correct Cartesian quadrant.
			var xNegResult = 512 + (y < 0 ? index : -index);
			var xPosResult = y < 0 ? 1024 - index : index;

			return new WAngle(x < 0 ? xNegResult : xPosResult);
		}

		// Must not be used outside rendering code
		public float RendererRadians() { return (float)(Angle * Math.PI / 512f); }
		public float RendererDegrees() { return Angle * 0.3515625f; }

		public override string ToString() { return Angle.ToStringInvariant(); }

		static readonly short[] CosineTable =
		[
			1024, 1023, 1023, 1023, 1023, 1023, 1023, 1023, 1022, 1022, 1022, 1021,
			1021, 1020, 1020, 1019, 1019, 1018, 1017, 1017, 1016, 1015, 1014, 1013,
			1012, 1011, 1010, 1009, 1008, 1007, 1006, 1005, 1004, 1003, 1001, 1000,
			999, 997, 996, 994, 993, 991, 990, 988, 986, 985, 983, 981, 979, 978,
			976, 974, 972, 970, 968, 966, 964, 962, 959, 957, 955, 953, 950, 948,
			946, 943, 941, 938, 936, 933, 930, 928, 925, 922, 920, 917, 914, 911,
			908, 906, 903, 900, 897, 894, 890, 887, 884, 881, 878, 875, 871, 868,
			865, 861, 858, 854, 851, 847, 844, 840, 837, 833, 829, 826, 822, 818,
			814, 811, 807, 803, 799, 795, 791, 787, 783, 779, 775, 771, 767, 762,
			758, 754, 750, 745, 741, 737, 732, 728, 724, 719, 715, 710, 706, 701,
			696, 692, 687, 683, 678, 673, 668, 664, 659, 654, 649, 644, 639, 634,
			629, 625, 620, 615, 609, 604, 599, 594, 589, 584, 579, 574, 568, 563,
			558, 553, 547, 542, 537, 531, 526, 521, 515, 510, 504, 499, 493, 488,
			482, 477, 471, 466, 460, 454, 449, 443, 437, 432, 426, 420, 414, 409,
			403, 397, 391, 386, 380, 374, 368, 362, 356, 350, 344, 339, 333, 327,
			321, 315, 309, 303, 297, 291, 285, 279, 273, 267, 260, 254, 248, 242,
			236, 230, 224, 218, 212, 205, 199, 193, 187, 181, 175, 168, 162, 156,
			150, 144, 137, 131, 125, 119, 112, 106, 100, 94, 87, 81, 75, 69, 62,
			56, 50, 43, 37, 31, 25, 18, 12, 6, 0
		];

		static readonly int[] TanTable =
		[
			0, 6, 12, 18, 25, 31, 37, 44, 50, 56, 62, 69, 75, 81, 88, 94, 100, 107,
			113, 119, 126, 132, 139, 145, 151, 158, 164, 171, 177, 184, 190, 197,
			203, 210, 216, 223, 229, 236, 243, 249, 256, 263, 269, 276, 283, 290,
			296, 303, 310, 317, 324, 331, 338, 345, 352, 359, 366, 373, 380, 387,
			395, 402, 409, 416, 424, 431, 438, 446, 453, 461, 469, 476, 484, 492,
			499, 507, 515, 523, 531, 539, 547, 555, 563, 571, 580, 588, 596, 605,
			613, 622, 630, 639, 648, 657, 666, 675, 684, 693, 702, 711, 721, 730,
			740, 749, 759, 769, 779, 789, 799, 809, 819, 829, 840, 850, 861, 872,
			883, 894, 905, 916, 928, 939, 951, 963, 974, 986, 999, 1011, 1023, 1036,
			1049, 1062, 1075, 1088, 1102, 1115, 1129, 1143, 1158, 1172, 1187, 1201,
			1216, 1232, 1247, 1263, 1279, 1295, 1312, 1328, 1345, 1363, 1380, 1398,
			1416, 1435, 1453, 1473, 1492, 1512, 1532, 1553, 1574, 1595, 1617, 1639,
			1661, 1684, 1708, 1732, 1756, 1782, 1807, 1833, 1860, 1887, 1915, 1944,
			1973, 2003, 2034, 2065, 2098, 2131, 2165, 2199, 2235, 2272, 2310, 2348,
			2388, 2429, 2472, 2515, 2560, 2606, 2654, 2703, 2754, 2807, 2861, 2918,
			2976, 3036, 3099, 3164, 3232, 3302, 3375, 3451, 3531, 3613, 3700, 3790,
			3885, 3984, 4088, 4197, 4311, 4432, 4560, 4694, 4836, 4987, 5147, 5318,
			5499, 5693, 5901, 6124, 6364, 6622, 6903, 7207, 7539, 7902, 8302, 8743,
			9233, 9781, 10396, 11094, 11891, 12810, 13882, 15148, 16667, 18524, 20843,
			23826, 27801, 33366, 41713, 55622, 83438, 166883, int.MaxValue
		];

		#region Scripting interface

		public LuaValue Add(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			if (!left.TryGetClrValue(out WAngle a))
				throw new LuaException(
					"Attempted to call WAngle.Add(WAngle, WAngle) with invalid arguments " +
					$"({left.WrappedClrType().Name}, {right.WrappedClrType().Name})");

			if (right.TryGetClrValue(out WAngle b))
				return new LuaCustomClrObject(a + b);

			throw new LuaException(
				"Attempted to call WAngle.Add(WAngle, WAngle) with invalid arguments " +
				$"({left.WrappedClrType().Name}, {right.WrappedClrType().Name})");
		}

		public LuaValue Subtract(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			if (!left.TryGetClrValue(out WAngle a))
				throw new LuaException(
					"Attempted to call WAngle.Subtract(WAngle, WAngle) with invalid arguments " +
					$"({left.WrappedClrType().Name}, {right.WrappedClrType().Name})");

			if (right.TryGetClrValue(out WAngle b))
				return new LuaCustomClrObject(a - b);

			throw new LuaException(
				"Attempted to call WAngle.Subtract(WAngle, WAngle) with invalid arguments " +
				$"({left.WrappedClrType().Name}, {right.WrappedClrType().Name})");
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			if (!left.TryGetClrValue(out WAngle a) || !right.TryGetClrValue(out WAngle b))
				return false;

			return a == b;
		}

		public LuaValue this[LuaRuntime runtime, LuaValue key]
		{
			get
			{
				switch (key.ToString())
				{
					case "Angle": return Angle;
					default: throw new LuaException($"WAngle does not define a member '{key}'");
				}
			}

			set => throw new LuaException("WAngle is read-only. Use Angle.New to create a new value");
		}

		public LuaValue ToString(LuaRuntime runtime) => ToString();

		#endregion
	}
}
