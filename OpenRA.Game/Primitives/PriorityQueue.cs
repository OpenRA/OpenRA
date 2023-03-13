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
	public interface IPriorityQueue<T>
	{
		void Add(T item);
		bool Empty { get; }
		T Peek();
		T Pop();
	}

	/// <summary>
	/// Represents a collection of items that have a priority.
	/// On pop, the item with the lowest priority value is removed.
	/// </summary>
	public sealed class PriorityQueue<T, TComparer> : IPriorityQueue<T> where TComparer : struct, IComparer<T>
	{
		/// <summary>
		/// Compares two items to determine their priority.
		/// PERF: Using a struct allows the calls to be devirtualized.
		/// </summary>
		readonly TComparer comparer;

		/// <summary>
		/// A <a href="https://en.wikipedia.org/wiki/Binary_heap">binary min-heap</a> storing the items.
		/// An array divided into sub arrays called levels. At each level the size of a level array doubles.
		/// Elements at deeper levels always have higher priority values than elements nearer to the root.
		/// </summary>
		T[] items;

		/// <summary>
		/// Index of deepest level.
		/// </summary>
		int level;

		/// <summary>
		/// Number of elements in the deepest level.
		/// </summary>
		int index;

		public PriorityQueue(TComparer comparer)
		{
			this.comparer = comparer;
			items = new T[1];
		}

		public void Add(T item)
		{
			var addLevel = level;
			var addIndex = index;

			while (addLevel >= 1)
			{
				var above = items[AboveIndex(addLevel, addIndex)];
				if (comparer.Compare(above, item) > 0)
				{
					items[Index(addLevel, addIndex)] = above;
					--addLevel;
					addIndex >>= 1;
				}
				else
					break;
			}

			items[Index(addLevel, addIndex)] = item;

			if (++index >= 1 << level)
			{
				index = 0;
				var count = 2 * (1 << ++level);
				if (count - 1 >= items.Length)
					Array.Resize(ref items, count);
			}
		}

		public bool Empty => level == 0;

		static int Index(int level, int index) { return (1 << level) - 1 + index; }

		static int AboveIndex(int level, int index) { return (1 << (level - 1)) - 1 + (index >> 1); }

		int IndexLast()
		{
			var lastLevel = level;
			var lastIndex = index;

			if (--lastIndex < 0)
				lastIndex = (1 << --lastLevel) - 1;

			return Index(lastLevel, lastIndex);
		}

		public T Peek()
		{
			if (level <= 0 && index <= 0)
				throw new InvalidOperationException("PriorityQueue empty.");

			return items[Index(0, 0)];
		}

		public T Pop()
		{
			var ret = Peek();
			BubbleInto(0, 0, items[IndexLast()]);
			if (--index < 0)
				index = (1 << --level) - 1;
			return ret;
		}

		void BubbleInto(int intoLevel, int intoIndex, T val)
		{
			while (true)
			{
				var downLevel = intoLevel + 1;
				var downIndex = intoIndex << 1;

				if (downLevel > level || (downLevel == level && downIndex >= index))
				{
					items[Index(intoLevel, intoIndex)] = val;
					return;
				}

				var down = items[Index(downLevel, downIndex)];
				if (downLevel < level || (downLevel == level && downIndex < index - 1))
				{
					var downRight = items[Index(downLevel, downIndex + 1)];
					if (comparer.Compare(down, downRight) >= 0)
					{
						down = downRight;
						++downIndex;
					}
				}

				if (comparer.Compare(val, down) <= 0)
				{
					items[Index(intoLevel, intoIndex)] = val;
					return;
				}

				items[Index(intoLevel, intoIndex)] = down;
				intoLevel = downLevel;
				intoIndex = downIndex;
			}
		}
	}
}
