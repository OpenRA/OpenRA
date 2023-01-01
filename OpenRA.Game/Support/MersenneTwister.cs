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

namespace OpenRA.Support
{
	// Quick & dirty Mersenne Twister [MT19937] implementation
	public class MersenneTwister
	{
		readonly uint[] mt = new uint[624];
		int index = 0;

		public int Last;
		public int TotalCount = 0;

		public MersenneTwister()
			: this(Environment.TickCount) { }

		public MersenneTwister(int seed)
		{
			mt[0] = (uint)seed;
			for (var i = 1u; i < mt.Length; i++)
				mt[i] = 1812433253u * (mt[i - 1] ^ (mt[i - 1] >> 30)) + i;
		}

		public int Next()
		{
			if (index == 0) Generate();

			var y = mt[index];
			y ^= y >> 11;
			y ^= (y << 7) & 2636928640;
			y ^= (y << 15) & 4022730752;
			y ^= y >> 18;

			index = (index + 1) % 624;
			TotalCount++;
			Last = (int)(y % int.MaxValue);
			return Last;
		}

		public int Next(int low, int high)
		{
			if (high < low)
				throw new ArgumentOutOfRangeException(nameof(high), "Maximum value is less than the minimum value.");

			var diff = high - low;
			if (diff <= 1)
				return low;

			return low + Next() % diff;
		}

		public int Next(int high)
		{
			return Next(0, high);
		}

		public float NextFloat()
		{
			return Math.Abs(Next() / (float)0x7fffffff);
		}

		void Generate()
		{
			unchecked
			{
				for (var i = 0u; i < mt.Length; i++)
				{
					var y = (mt[i] & 0x80000000) | (mt[(i + 1) % 624] & 0x7fffffff);
					mt[i] = mt[(i + 397u) % 624u] ^ (y >> 1);
					if ((y & 1) == 1)
						mt[i] = mt[i] ^ 2567483615;
				}
			}
		}
	}
}
