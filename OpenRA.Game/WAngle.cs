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
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Scripting;

namespace OpenRA
{
	/// <summary>
	/// 1D angle - 1024 units = 360 degrees.
	/// </summary>
	public readonly struct WAngle : IScriptBindable, ILuaAdditionBinding, ILuaSubtractionBinding, ILuaEqualityBinding, IEquatable<WAngle>
	{
		public readonly int Angle;
		public int AngleSquared => (int)Angle * Angle;

		public WAngle(int a)
		{
			Angle = a % 1024;
			if (Angle < 0)
				Angle += 1024;
		}

		public static readonly WAngle Zero = new WAngle(0);
		public static WAngle FromFacing(int facing) { return new WAngle(facing * 4); }
		public static WAngle FromDegrees(int degrees) { return new WAngle(degrees * 1024 / 360); }
		public static WAngle operator +(WAngle a, WAngle b) { return new WAngle(a.Angle + b.Angle); }
		public static WAngle operator -(WAngle a, WAngle b) { return new WAngle(a.Angle - b.Angle); }
		public static WAngle operator -(WAngle a) { return new WAngle(-a.Angle); }

		public static bool operator ==(WAngle me, WAngle other) { return me.Angle == other.Angle; }
		public static bool operator !=(WAngle me, WAngle other) { return !(me == other); }

		public override int GetHashCode() { return Angle.GetHashCode(); }

		public bool Equals(WAngle other) { return other == this; }
		public override bool Equals(object obj) { return obj is WAngle && Equals((WAngle)obj); }

		public int Facing => Angle / 4;

		public int Sin() { return new WAngle(Angle - 256).Cos(); }

		public int Cos()
		{
			if (Angle <= 256)
				return CosineTable[Angle];
			if (Angle <= 512)
				return -CosineTable[512 - Angle];
			return -new WAngle(Angle - 512).Cos();
		}

		public int Tan()
		{
			if (Angle <= 256)
				return TanTable[Angle];
			if (Angle <= 512)
				return -TanTable[512 - Angle];
			return new WAngle(Angle - 512).Tan();
		}

		public static WAngle Lerp(WAngle a, WAngle b, int mul, int div)
		{
			// Map 1024 <-> 0 wrapping into linear space
			var aa = a.Angle;
			var bb = b.Angle;
			if (aa > bb && aa - bb > 512)
				aa -= 1024;

			if (bb > aa && bb - aa > 512)
				bb -= 1024;

			return new WAngle(aa + (bb - aa) * mul / div);
		}

		public static WAngle ArcSin(int d)
		{
			if (d < -1024 || d > 1024)
				throw new ArgumentException($"ArcSin is only valid for values between -1024 and 1024. Received {d}");

			var a = ClosestCosineIndex(Math.Abs(d));
			return new WAngle(d < 0 ? 768 + a : 256 - a);
		}

		public static WAngle ArcCos(int d)
		{
			if (d < -1024 || d > 1024)
				throw new ArgumentException($"ArcCos is only valid for values between -1024 and 1024. Received {d}");

			var a = ClosestCosineIndex(Math.Abs(d));
			return new WAngle(d < 0 ? 512 - a : a);
		}

		/// <summary>
		/// Find the index of CosineTable that has the value closest to the given value.
		/// The first or last index will be returned for values above or below the valid range
		/// </summary>
		static int ClosestCosineIndex(int value)
		{
			var aboveIndex = 0;
			var belowIndex = 256;
			while (aboveIndex != belowIndex - 1)
			{
				var index = (aboveIndex + belowIndex) / 2;
				var val = CosineTable[index];

				if (val == value)
					return index;

				if (val < value)
					belowIndex = index;
				else
					aboveIndex = index;
			}

			// Take the index with the smallest error
			return CosineTable[aboveIndex] - value > value - CosineTable[belowIndex] ? belowIndex : aboveIndex;
		}

		public static WAngle ArcTan(int y, int x) { return ArcTan(y, x, 1); }
		public static WAngle ArcTan(int y, int x, int stride)
		{
			if (y == 0)
				return new WAngle(x >= 0 ? 0 : 512);

			if (x == 0)
				return new WAngle(Math.Sign(y) * 256);

			var ay = Math.Abs(y);
			var ax = Math.Abs(x);

			// Find the closest angle that satisfies y = x*tan(theta)
			// Uses a long to store bestVal to eliminate integer overflow issues in the common cases
			// (may still fail for unrealistically large ax and ay)
			var bestVal = long.MaxValue;
			var bestAngle = 0;
			for (var i = 0; i < 256; i += stride)
			{
				var val = Math.Abs(1024 * ay - (long)ax * TanTable[i]);
				if (val < bestVal)
				{
					bestVal = val;
					bestAngle = i;
				}
			}

			// Calculate quadrant
			if (x < 0 && y > 0)
				bestAngle = 512 - bestAngle;
			else if (x < 0 && y < 0)
				bestAngle = 512 + bestAngle;
			else if (x > 0 && y < 0)
				bestAngle = 1024 - bestAngle;

			return new WAngle(bestAngle);
		}

		// Must not be used outside rendering code
		public float RendererRadians() { return (float)(Angle * Math.PI / 512f); }
		public float RendererDegrees() { return Angle * 0.3515625f; }

		public override string ToString() { return Angle.ToString(); }

		static readonly int[] CosineTable =
		{
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
		};

		static readonly int[] TanTable =
		{
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
		};

		#region Scripting interface

		public LuaValue Add(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			if (!left.TryGetClrValue(out WAngle a))
				throw new LuaException($"Attempted to call WAngle.Add(WAngle, WAngle) with invalid arguments ({left.WrappedClrType().Name}, {right.WrappedClrType().Name})");

			if (right.TryGetClrValue(out WAngle b))
				return new LuaCustomClrObject(a + b);

			throw new LuaException($"Attempted to call WAngle.Add(WAngle, WAngle) with invalid arguments ({left.WrappedClrType().Name}, {right.WrappedClrType().Name})");
		}

		public LuaValue Subtract(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			if (!left.TryGetClrValue(out WAngle a))
				throw new LuaException($"Attempted to call WAngle.Subtract(WAngle, WAngle) with invalid arguments ({left.WrappedClrType().Name}, {right.WrappedClrType().Name})");

			if (right.TryGetClrValue(out WAngle b))
				return new LuaCustomClrObject(a - b);

			throw new LuaException($"Attempted to call WAngle.Subtract(WAngle, WAngle) with invalid arguments ({left.WrappedClrType().Name}, {right.WrappedClrType().Name})");
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			if (!left.TryGetClrValue(out WAngle a) || !right.TryGetClrValue(out WAngle b))
				return false;

			return a == b;
		}

		#endregion
	}
}
