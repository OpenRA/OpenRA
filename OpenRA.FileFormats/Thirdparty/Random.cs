#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;

namespace OpenRA.Thirdparty
{
	// quick & dirty Mersenne Twister [MT19937] implementation

	public class Random
	{
		uint[] mt = new uint[624];
		int index = 0;
		
		public int Last;

		public Random() : this(Environment.TickCount) { }
		
		public Random(int seed)
		{
			mt[0] = (uint)seed;
			for (var i = 1u; i < mt.Length; i++)
				mt[i] = 1812433253u * (mt[i - 1] ^ (mt[i - 1] >> 30)) + i;
		}

		public int Next()
		{
			if (index == 0) Generate();

			var y = mt[index];
			y ^= (y >> 11);
			y ^= ((y << 7) & 2636928640);
			y ^= ((y << 15) & 4022730752);
			y ^= y >> 18;

			index = (index + 1) % 624;
			Last = (int)(y % int.MaxValue);
			return Last;
		}

		public int Next(int low, int high) { return low + Next() % (high - low); }
		public int Next(int high) { return Next() % high; }
		public double NextDouble() { return Math.Abs(Next() / (double)0x7fffffff); }

		void Generate()
		{
			unchecked
			{
				for (var i = 0u; i < mt.Length; i++)
				{
					var y = (mt[i] & 0x80000000) | (mt[(i + 1) % 624] & 0x7fffffff);
					mt[i] = mt[(i + 397u) % 624u] ^ (y >> 1);
					if ((y & 1) == 1)
						mt[i] = (mt[i] ^ 2567483615);
				}
			}
		}
	}
}
