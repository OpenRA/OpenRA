#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRa.FileFormats
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
			int addLevel = level;
			int addIndex = index;

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

		public bool Empty { get { return (level == 0); } }

		T At(int level, int index) { return items[level][index]; }
		T Above(int level, int index) { return items[level - 1][index >> 1]; }

		T Last()
		{
			int lastLevel = level;
			int lastIndex = index;

			if (--lastIndex < 0)
				lastIndex = (1 << --lastLevel) - 1;

			return At(lastLevel, lastIndex);
		}

		public T Pop()
		{
			if (level == 0 && index == 0)
				throw new InvalidOperationException("Attempting to pop empty PriorityQueue");

			T ret = At(0, 0);
			BubbleInto(0, 0, Last());
			if (--index < 0)
				index = (1 << --level) - 1;

			return ret;
		}

		void BubbleInto(int intoLevel, int intoIndex, T val)
		{
			int downLevel = intoLevel + 1;
			int downIndex = intoIndex << 1;

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

		int RowLength(int i)
		{
			if (i == level)
				return index;
			return (1 << i);
		}
	}
}
