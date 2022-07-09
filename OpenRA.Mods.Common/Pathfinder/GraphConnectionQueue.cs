#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Runtime.CompilerServices;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// Priority queue that stores graph connections in priority of their cost.
	/// PERF: Do not use generics. Compare cost fields directly. Use an array for storage over a list of arrays.
	/// </summary>
	public sealed class GraphConnectionQueue
	{
		/// <summary>
		/// Array devided into sub arrays called levels. At each level the size of a level array doubles.
		/// Elements stored one one level are near sorted. The top level array at index zero
		/// has length one and stores the element with the higest priority.
		/// After popping the top element, elements from lower level bubble up into higher levels.
		/// </summary>
		GraphConnection[] items;

		/// <summary>
		/// Index of deepest level.
		/// </summary>
		int level;

		/// <summary>
		/// Number of elements in the deepest level array.
		/// </summary>
		int index;

		// Intial capacity of 512 matches eight levels.
		public GraphConnectionQueue(int initialCapacity = 512)
		{
			items = new GraphConnection[initialCapacity > 0 ? initialCapacity : 1];
		}

		public void Add(GraphConnection item)
		{
			var span = items.AsSpan();
			var addLevel = level;
			var addIndex = index;

			while (addLevel >= 1)
			{
				var above = span[AboveIndex(addLevel, addIndex)];
				if (above.Cost > item.Cost)
				{
					span[Index(addLevel, addIndex)] = above;
					--addLevel;
					addIndex >>= 1;
				}
				else
					break;
			}

			span[Index(addLevel, addIndex)] = item;

			if (++index >= (1 << level))
			{
				index = 0;
				var levelWidth = 1 << ++level;
				if (levelWidth + levelWidth - 1 >= items.Length)
					GrowCapacity();
			}
		}

		void GrowCapacity()
		{
			var newItems = new GraphConnection[(1 << level) * 2];
			Array.Copy(items, newItems, items.Length);
			items = newItems;
		}

		public bool Empty => level == 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int Index(int level, int index) { return ((1 << level) - 1) + index; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int AboveIndex(int level, int index) { return ((1 << (level - 1)) - 1) + (index >> 1); }

		int IndexLast()
		{
			var lastLevel = level;
			var lastIndex = index;

			if (--lastIndex < 0)
				lastIndex = (1 << --lastLevel) - 1;

			return Index(lastLevel, lastIndex);
		}

		public GraphConnection Peek()
		{
			if (level <= 0 && index <= 0)
				throw new InvalidOperationException("PriorityQueue empty.");

			// PERF: Index(0, 0) = 0
			return items[0];
		}

		public GraphConnection Pop()
		{
			var ret = Peek();
			BubbleInto(0, 0, items[IndexLast()]);
			if (--index < 0)
				index = (1 << --level) - 1;
			return ret;
		}

		void BubbleInto(int intoLevel, int intoIndex, GraphConnection val)
		{
			var span = items.AsSpan();
			while (true)
			{
				var downLevel = intoLevel + 1;
				var downIndex = intoIndex << 1;

				if (downLevel > level || (downLevel == level && downIndex >= index))
				{
					span[Index(intoLevel, intoIndex)] = val;
					return;
				}

				var down = span[Index(downLevel, downIndex)];
				if (downLevel < level || (downLevel == level && downIndex < index - 1))
				{
					var downRight = span[Index(downLevel, downIndex + 1)];
					if (down.Cost >= downRight.Cost)
					{
						down = downRight;
						++downIndex;
					}
				}

				if (val.Cost <= down.Cost)
				{
					span[Index(intoLevel, intoIndex)] = val;
					return;
				}

				span[Index(intoLevel, intoIndex)] = down;
				intoLevel = downLevel;
				intoIndex = downIndex;
			}
		}
	}
}
