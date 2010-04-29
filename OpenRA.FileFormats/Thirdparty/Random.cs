using System;

namespace OpenRA.Thirdparty
{
	// quick & dirty Mersenne Twister [MT19937] implementation

	public class Random
	{
		int[] mt = new int[624];
		int index = 0;

		public Random() : this(Environment.TickCount) { }
		
		public Random(int seed)
		{
			mt[0] = seed;
			for (var i = 1; i < mt.Length; i++)
				mt[i] = 1812433253 * (mt[i - 1] ^ (mt[i - 1] >> 30)) + i;
		}

		public int Next()
		{
			if (index == 0) Generate();

			var y = mt[index];
			y ^= (y >> 11);
			y ^= (int)((y << 7) & 2636928640);
			y ^= (int)((y << 15) & 4022730752);
			y ^= y >> 18;

			index = (index + 1) % 624;
			return y;
		}

		public int Next(int low, int high) { return low + Next() % (high - low); }
		public int Next(int high) { return Next() % high; }
		public double NextDouble() { return (uint)Next() / (double)uint.MaxValue; }

		void Generate()
		{
			for (var i = 0; i < mt.Length; i++)
			{
				var y = (mt[i] & int.MinValue) | (mt[(i + 1) % 624] & int.MaxValue);
				mt[i] = mt[(i + 397) % 624] ^ (y >> 1);
				if ((y & 1) == 1)
					mt[i] = (int)(mt[i] ^ 2567483615);
			}
		}
	}
}
