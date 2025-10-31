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

namespace OpenRA.Primitives
{
	/// <summary>
	/// A random-access array which keeps track of the minimum item.
	/// </summary>
	public sealed class PriorityArray<T> where T : IComparable<T>
	{
		readonly T[] items;

		readonly int[] itemIndexToHeapIndex;
		readonly int[] heapOfItemIndices;

		/// <summary>
		/// Create a new PriorityArray of given size with all values preset to the given init value.
		/// </summary>
		public PriorityArray(int size, T init)
		{
			items = new T[size];
			itemIndexToHeapIndex = new int[size];
			heapOfItemIndices = new int[size];
			Array.Fill(items, init);
			for (var i = 0; i < size; i++)
			{
				items[i] = init;
				itemIndexToHeapIndex[i] = i;
				heapOfItemIndices[i] = i;
			}
		}

		public int Length => items.Length;

		/// <summary>Get the index of the minimum element.</summary>
		public int GetMinIndex() => heapOfItemIndices[0];

		public T this[int itemIndex]
		{
			get => items[itemIndex];
			set
			{
				items[itemIndex] = value;
				Bubble(itemIndexToHeapIndex[itemIndex]);
			}
		}

		void Bubble(int heapIndex)
		{
			if (!BubbleUp(heapIndex))
			{
				BubbleDown(heapIndex);
			}
		}

		bool BubbleUp(int heapIndex)
		{
			if (heapIndex == 0)
				return false;
			var itemIndex = heapOfItemIndices[heapIndex];
			var upHeapIndex = ((heapIndex + 1) >> 1) - 1;
			var upItemIndex = heapOfItemIndices[upHeapIndex];
			if (items[itemIndex].CompareTo(items[upItemIndex]) >= 0)
				return false;
			Swap(heapIndex, upHeapIndex, itemIndex, upItemIndex);
			BubbleUp(upHeapIndex);
			return true;
		}

		bool BubbleDown(int heapIndex)
		{
			var itemIndex = heapOfItemIndices[heapIndex];
			var leftDownHeapIndex = ((heapIndex + 1) << 1) - 1;
			var rightDownHeapIndex = leftDownHeapIndex + 1;
			if (leftDownHeapIndex >= Length)
				return false;
			var item = items[itemIndex];
			if (rightDownHeapIndex < Length)
			{
				var leftDownItemIndex = heapOfItemIndices[leftDownHeapIndex];
				var rightDownItemIndex = heapOfItemIndices[rightDownHeapIndex];
				var leftDownItem = items[leftDownItemIndex];
				var rightDownItem = items[rightDownItemIndex];
				if (item.CompareTo(leftDownItem) <= 0 && item.CompareTo(rightDownItem) <= 0)
					return false;
				if (leftDownItem.CompareTo(rightDownItem) <= 0)
				{
					Swap(heapIndex, leftDownHeapIndex, itemIndex, leftDownItemIndex);
					BubbleDown(leftDownHeapIndex);
				}
				else
				{
					Swap(heapIndex, rightDownHeapIndex, itemIndex, rightDownItemIndex);
					BubbleDown(rightDownHeapIndex);
				}

				return true;
			}
			else
			{
				// Just the left down one exists.
				var leftDownItemIndex = heapOfItemIndices[leftDownHeapIndex];
				var leftDownItem = items[leftDownItemIndex];
				if (item.CompareTo(leftDownItem) <= 0)
					return false;
				Swap(heapIndex, leftDownHeapIndex, itemIndex, leftDownItemIndex);
				BubbleDown(leftDownHeapIndex);
				return true;
			}
		}

		void Swap(int heapIndex1, int heapIndex2, int itemIndex1, int itemIndex2)
		{
			(itemIndexToHeapIndex[itemIndex1], itemIndexToHeapIndex[itemIndex2]) =
				(itemIndexToHeapIndex[itemIndex2], itemIndexToHeapIndex[itemIndex1]);
			(heapOfItemIndices[heapIndex1], heapOfItemIndices[heapIndex2]) =
				(heapOfItemIndices[heapIndex2], heapOfItemIndices[heapIndex1]);
		}
	}
}
