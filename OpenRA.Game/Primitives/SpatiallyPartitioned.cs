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
using System.Collections.Generic;

namespace OpenRA.Primitives
{
	public sealed class SpatiallyPartitioned<T>
	{
		readonly int rows, cols, binSize;
		readonly Dictionary<T, Rectangle>[] itemBoundsBins;
		readonly Dictionary<T, Rectangle> itemBounds = new Dictionary<T, Rectangle>();
		readonly Action<Dictionary<T, Rectangle>, T, Rectangle> addItem = (bin, actor, bounds) => bin.Add(actor, bounds);
		readonly Action<Dictionary<T, Rectangle>, T, Rectangle> removeItem = (bin, actor, bounds) => bin.Remove(actor);

		public SpatiallyPartitioned(int width, int height, int binSize)
		{
			this.binSize = binSize;
			rows = Exts.IntegerDivisionRoundingAwayFromZero(height, binSize);
			cols = Exts.IntegerDivisionRoundingAwayFromZero(width, binSize);
			itemBoundsBins = Exts.MakeArray(rows * cols, _ => new Dictionary<T, Rectangle>());
		}

		void ValidateBounds(T actor, Rectangle bounds)
		{
			if (bounds.Width == 0 || bounds.Height == 0)
				throw new ArgumentException($"Bounds of actor {actor} are empty.", nameof(bounds));
		}

		public void Add(T item, Rectangle bounds)
		{
			ValidateBounds(item, bounds);
			itemBounds.Add(item, bounds);
			MutateBins(item, bounds, addItem);
		}

		public void Update(T item, Rectangle bounds)
		{
			ValidateBounds(item, bounds);
			MutateBins(item, itemBounds[item], removeItem);
			MutateBins(item, itemBounds[item] = bounds, addItem);
		}

		public bool Remove(T item)
		{
			if (!itemBounds.TryGetValue(item, out var bounds))
				return false;

			MutateBins(item, bounds, removeItem);
			itemBounds.Remove(item);
			return true;
		}

		public bool Contains(T item)
		{
			return itemBounds.ContainsKey(item);
		}

		Dictionary<T, Rectangle> BinAt(int row, int col)
		{
			return itemBoundsBins[row * cols + col];
		}

		Rectangle BinBounds(int row, int col)
		{
			return new Rectangle(col * binSize, row * binSize, binSize, binSize);
		}

		void BoundsToBinRowsAndCols(Rectangle bounds, out int minRow, out int maxRow, out int minCol, out int maxCol)
		{
			var top = Math.Min(bounds.Top, bounds.Bottom);
			var bottom = Math.Max(bounds.Top, bounds.Bottom);
			var left = Math.Min(bounds.Left, bounds.Right);
			var right = Math.Max(bounds.Left, bounds.Right);

			minRow = Math.Max(0, top / binSize);
			minCol = Math.Max(0, left / binSize);
			maxRow = Math.Min(rows, Exts.IntegerDivisionRoundingAwayFromZero(bottom, binSize));
			maxCol = Math.Min(cols, Exts.IntegerDivisionRoundingAwayFromZero(right, binSize));
		}

		void MutateBins(T actor, Rectangle bounds, Action<Dictionary<T, Rectangle>, T, Rectangle> action)
		{
			BoundsToBinRowsAndCols(bounds, out var minRow, out var maxRow, out var minCol, out var maxCol);

			for (var row = minRow; row < maxRow; row++)
				for (var col = minCol; col < maxCol; col++)
					action(BinAt(row, col), actor, bounds);
		}

		public IEnumerable<T> At(int2 location)
		{
			var col = (location.X / binSize).Clamp(0, cols - 1);
			var row = (location.Y / binSize).Clamp(0, rows - 1);
			foreach (var kvp in BinAt(row, col))
				if (kvp.Value.Contains(location))
					yield return kvp.Key;
		}

		public IEnumerable<T> InBox(Rectangle box)
		{
			BoundsToBinRowsAndCols(box, out var minRow, out var maxRow, out var minCol, out var maxCol);

			// We want to return any items intersecting the box.
			// If the box covers multiple bins, we must handle items that are contained in multiple bins and avoid
			// returning them more than once. We shall use a set to track these.
			// PERF: If we are only looking inside one bin, we can avoid the cost of performing this tracking.
			var items = minRow >= maxRow || minCol >= maxCol ? null : new HashSet<T>();
			for (var row = minRow; row < maxRow; row++)
				for (var col = minCol; col < maxCol; col++)
				{
					var binBounds = BinBounds(row, col);
					foreach (var kvp in BinAt(row, col))
					{
						var item = kvp.Key;
						var bounds = kvp.Value;

						// If the item is in the bin, we must check it intersects the box before returning it.
						// We shall track it in the set of items seen so far to avoid returning it again if it appears
						// in another bin.
						// PERF: If the item is wholly contained within the bin, we can avoid the cost of tracking it.
						if (bounds.IntersectsWith(box) &&
							(items == null || binBounds.Contains(bounds) || items.Add(item)))
							yield return item;
					}
				}
		}

		public IEnumerable<Rectangle> ItemBounds => itemBounds.Values;

		public IEnumerable<T> Items => itemBounds.Keys;
	}
}
