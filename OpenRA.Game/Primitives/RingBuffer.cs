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

namespace OpenRA.Primitives
{
	/// <summary>Fixed size rorating buffer backed by an array.</summary>
	public class RingBuffer<T> : ICollection<T>, IEnumerable<T>
	{
		readonly IComparer<T> comparer;
		readonly T[] values;
		int start;

		public int Capacity => values.Length;
		public int Count { get; private set; }
		public bool IsReadOnly => false;

		public RingBuffer(int capacity, IComparer<T> comparer)
		{
			this.comparer = comparer;
			values = new T[capacity];
			start = 0;
			Count = 0;
		}

		public RingBuffer(int capacity)
			: this(capacity, Comparer<T>.Default) { }

		public void Add(T value)
		{
			values[(start + Count) % values.Length] = value;
			if (Count < values.Length)
				Count++;
			else
				start = (start + 1) % values.Length;
		}

		public void Clear()
		{
			Array.Clear(values, 0, values.Length);
			start = 0;
			Count = 0;
		}

		public bool Contains(T value)
		{
			var capacity = values.Length;
			var end = start + Count;
			for (var i = start; i < end; ++i)
				if (comparer.Compare(values[i % capacity], value) == 0)
					return true;

			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));

			if (arrayIndex < 0)
				throw new ArgumentNullException(nameof(arrayIndex));

			if (arrayIndex + Count >= array.Length)
				throw new ArgumentException("Invalid array capacity");

			var destinationIndex = arrayIndex;
			var end = start + Count;
			var capacity = values.Length;
			for (var i = start; i < end; ++i)
				array[destinationIndex++] = values[i % capacity];
		}

		public bool Remove(T value)
		{
			var capacity = values.Length;
			var end = start + Count;
			for (var i = start; i < end; ++i)
			{
				if (comparer.Compare(values[i % capacity], value) == 0)
				{
					end--;
					for (var j = i; j < end; ++j)
						values[j % capacity] = values[(j + 1) % capacity];

					Count--;
					return true;
				}
			}

			return false;
		}

		public T this[int pos]
		{
			get => values[(start + pos) % values.Length];

			set
			{
				if (pos >= Count)
					throw new ArgumentException($"Index out of bounds: {pos}");

				values[(start + pos) % values.Length] = value;
			}
		}

		public T First()
		{
			if (Count == 0)
				throw new ArgumentException("Empty buffer");

			return values[start];
		}

		public T Last()
		{
			if (Count == 0)
				throw new ArgumentException("Empty buffer");

			return values[(start + Count - 1) % values.Length];
		}

		public IEnumerator<T> GetEnumerator()
		{
			var initState = start + Count;
			for (var i = 0; i < Count; i++)
			{
				if (start + Count != initState)
					throw new InvalidOperationException("Collection was modified; enumeration operation may not execute");
				yield return values[(start + i) % values.Length];
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
