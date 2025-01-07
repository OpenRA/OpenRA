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
using System.Collections.Generic;
using System.Linq;

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

		/// <summary>
		/// Produces an unsigned integer between -0x80000000 and 0x7fffffff inclusive.
		/// </summary>
		public uint NextUint()
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
			return y;
		}

		/// <summary>
		/// Produces an unsigned integer between -0x80000000 and 0x7fffffff inclusive.
		/// </summary>
		public ulong NextUlong()
		{
			return (ulong)NextUint() << 32 | NextUint();
		}

		/// <summary>
		/// Produces signed integers between -0x7fffffff and 0x7fffffff inclusive.
		/// 0 is twice as likely as any other number.
		/// </summary>
		public int Next()
		{
			NextUint();
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

		/// <summary>
		/// Produces random 32-bit floats between 0 inclusive and 1 inclusive.
		/// Note that whilst floats are 32-bit (23-bit mantissa), the entropy is not. Lower numbers preserve more entropy.
		/// </summary>
		public float NextFloat()
		{
			return Math.Abs(Next() / (float)0x7fffffff);
		}

		/// <summary>
		/// Produces uniformally distributed random floats between 0 inclusive and 1 exclusive.
		/// Note that whilst floats are 32-bit (23-bit mantissa), each output contains exactly 23 bits of entropy.
		/// </summary>
		public float NextFloatExclusive()
		{
			return (NextUint() & 0x7fffff) / (float)0x800000;
		}

		/// <summary>
		/// Produces uniformally distributed random doubles between 0 inclusive and 1 exclusive.
		/// Note that whilst doubles are 64-bit (52-bit mantissa), each output contains exactly 52 bits of entropy.
		/// </summary>
		public double NextDoubleExclusive()
		{
			return (NextUlong() & 0xfffffffffffffL) / (double)0x10000000000000L;
		}

		/// <summary>
		/// Pick a random an index from a list of weights.
		/// </summary>
		public int PickWeighted(IReadOnlyList<float> weights)
		{
			var total = weights.Sum();
			var spin = NextFloatExclusive() * total;
			int i;
			float acc = 0;
			for (i = 0; i < weights.Count; i++)
			{
				acc += weights[i];
				if (spin < acc)
					return i;
			}

			// This might be possible due to floating point precision loss
			// (in rare cases). Or we might have been given rubbish
			// weights. Return anything > 0.
			for (i = 0; i < weights.Count; i++)
				if (weights[i] > 0)
					return i;

			// All <= 0!
			return Next(0, weights.Count);
		}

		/// <summary>
		/// Shuffle a portion of a list list in place. Has minor biases.
		/// </summary>
		public void ShuffleInPlace<T>(IList<T> list, int start, int len)
		{
			for (var i = len; i > 1; i--)
			{
				var swap = Next(i);
				(list[start + i - 1], list[start + swap]) =
					(list[start + swap], list[start + i - 1]);
			}
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
						mt[i] ^= 2567483615;
				}
			}
		}
	}
}
