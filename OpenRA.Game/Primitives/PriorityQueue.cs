#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Primitives
{
	public class PriorityQueue<T>
			where T : IComparable<T>
	{
		List<T[]> items = new List<T[]>();
		int level, index;

		public PriorityQueue()
		{
			items.Add(new T[1]);
		}

		public void Add(T item)
		{
			var addLevel = level;
			var addIndex = index;

			while (addLevel >= 1 && Above(addLevel, addIndex).CompareTo(item) > 0)
			{
				items[addLevel][addIndex] = Above(addLevel, addIndex);
				--addLevel;
				addIndex >>= 1;
			}

			items[addLevel][addIndex] = item;

			if (++index >= (1 << level))
			{
				index = 0;
				if (items.Count <= ++level)
					items.Add(new T[1 << level]);
			}
		}

		public bool Empty { get { return level == 0; } }

		T At(int level, int index) { return items[level][index]; }
		T Above(int level, int index) { return items[level - 1][index >> 1]; }

		T Last()
		{
			var lastLevel = level;
			var lastIndex = index;

			if (--lastIndex < 0)
				lastIndex = (1 << --lastLevel) - 1;

			return At(lastLevel, lastIndex);
		}

		public T Peek() { return At(0, 0); }
		public T Pop()
		{
			if (level == 0 && index == 0)
				throw new InvalidOperationException("Attempting to pop empty PriorityQueue");

			var ret = At(0, 0);
			BubbleInto(0, 0, Last());
			if (--index < 0)
				index = (1 << --level) - 1;

			return ret;
		}

		void BubbleInto(int intoLevel, int intoIndex, T val)
		{
			var downLevel = intoLevel + 1;
			var downIndex = intoIndex << 1;

			if (downLevel > level || (downLevel == level && downIndex >= index))
			{
				items[intoLevel][intoIndex] = val;
				return;
			}

			if (downLevel <= level && downIndex < index - 1 &&
				At(downLevel, downIndex).CompareTo(At(downLevel, downIndex + 1)) >= 0)
				++downIndex;

			if (val.CompareTo(At(downLevel, downIndex)) <= 0)
			{
				items[intoLevel][intoIndex] = val;
				return;
			}

			items[intoLevel][intoIndex] = At(downLevel, downIndex);
			BubbleInto(downLevel, downIndex, val);
		}
	}
}
