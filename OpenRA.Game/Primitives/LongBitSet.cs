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
using System.Collections;
using System.Collections.Generic;

namespace OpenRA.Primitives
{
	static class LongBitSetAllocator<T> where T : class
	{
		static readonly Cache<string, long> Bits = new Cache<string, long>(Allocate);
		static long nextBits = 1;

		static long Allocate(string value)
		{
			lock (Bits)
			{
				var bits = nextBits;
				nextBits <<= 1;

				if (nextBits == 0)
					throw new OverflowException("Trying to allocate bit index outside of index 64.");

				return bits;
			}
		}

		public static long GetBits(string[] values)
		{
			long bits = 0;
			lock (Bits)
				foreach (var value in values)
					bits |= Bits[value];

			return bits;
		}

		public static long GetBitsNoAlloc(string[] values)
		{
			// Map strings to existing bits; do not allocate missing values new bits
			long bits = 0;

			lock (Bits)
				foreach (var value in values)
					if (Bits.TryGetValue(value, out var valueBit))
						bits |= valueBit;

			return bits;
		}

		public static IEnumerable<string> GetStrings(long bits)
		{
			var values = new List<string>();
			lock (Bits)
				foreach (var kvp in Bits)
					if ((kvp.Value & bits) != 0)
						values.Add(kvp.Key);

			return values;
		}

		public static bool BitsContainString(long bits, string value)
		{
			long valueBit;
			lock (Bits)
				if (!Bits.TryGetValue(value, out valueBit))
					return false;
			return (bits & valueBit) != 0;
		}

		public static void Reset()
		{
			lock (Bits)
			{
				Bits.Clear();
				nextBits = 1;
			}
		}
	}

	// Optimized BitSet to be used only when guaranteed to be no more than 64 values.
	public readonly struct LongBitSet<T> : IEnumerable<string>, IEquatable<LongBitSet<T>> where T : class
	{
		readonly long bits;

		public LongBitSet(params string[] values)
			: this(LongBitSetAllocator<T>.GetBits(values)) { }

		LongBitSet(long bits) { this.bits = bits; }

		public static LongBitSet<T> FromStringsNoAlloc(string[] values)
		{
			return new LongBitSet<T>(LongBitSetAllocator<T>.GetBitsNoAlloc(values)) { };
		}

		public static void Reset()
		{
			LongBitSetAllocator<T>.Reset();
		}

		public override string ToString()
		{
			return LongBitSetAllocator<T>.GetStrings(bits).JoinWith(",");
		}

		public static bool operator ==(LongBitSet<T> me, LongBitSet<T> other) { return me.bits == other.bits; }
		public static bool operator !=(LongBitSet<T> me, LongBitSet<T> other) { return !(me == other); }

		public bool Equals(LongBitSet<T> other) { return other == this; }
		public override bool Equals(object obj) { return obj is LongBitSet<T> && Equals((LongBitSet<T>)obj); }
		public override int GetHashCode() { return bits.GetHashCode(); }

		public bool IsEmpty => bits == 0;

		public bool IsProperSubsetOf(LongBitSet<T> other)
		{
			return IsSubsetOf(other) && !SetEquals(other);
		}

		public bool IsProperSupersetOf(LongBitSet<T> other)
		{
			return IsSupersetOf(other) && !SetEquals(other);
		}

		public bool IsSubsetOf(LongBitSet<T> other)
		{
			return (bits | other.bits) == other.bits;
		}

		public bool IsSupersetOf(LongBitSet<T> other)
		{
			return (bits | other.bits) == bits;
		}

		public bool Overlaps(LongBitSet<T> other)
		{
			return (bits & other.bits) != 0;
		}

		public bool SetEquals(LongBitSet<T> other)
		{
			return bits == other.bits;
		}

		public bool Contains(string value)
		{
			return LongBitSetAllocator<T>.BitsContainString(bits, value);
		}

		public IEnumerator<string> GetEnumerator()
		{
			return LongBitSetAllocator<T>.GetStrings(bits).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public LongBitSet<T> Except(LongBitSet<T> other)
		{
			return new LongBitSet<T>(bits & ~other.bits);
		}

		public LongBitSet<T> Intersect(LongBitSet<T> other)
		{
			return new LongBitSet<T>(bits & other.bits);
		}

		public LongBitSet<T> SymmetricExcept(LongBitSet<T> other)
		{
			return new LongBitSet<T>(bits ^ other.bits);
		}

		public LongBitSet<T> Union(LongBitSet<T> other)
		{
			return new LongBitSet<T>(bits | other.bits);
		}
	}
}
