﻿#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;

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

		void ValidateBounds(Rectangle bounds)
		{
			if (bounds.Width <= 0 || bounds.Height <= 0)
				throw new ArgumentException("bounds must be non-empty.", "bounds");
		}

		public void Add(T item, Rectangle bounds)
		{
			ValidateBounds(bounds);
			itemBounds.Add(item, bounds);
			MutateBins(item, bounds, addItem);
		}

		public void Update(T item, Rectangle bounds)
		{
			ValidateBounds(bounds);
			MutateBins(item, itemBounds[item], removeItem);
			MutateBins(item, itemBounds[item] = bounds, addItem);
		}

		public void Remove(T item)
		{
			MutateBins(item, itemBounds[item], removeItem);
			itemBounds.Remove(item);
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
			minRow = Math.Max(0, bounds.Top / binSize);
			minCol = Math.Max(0, bounds.Left / binSize);
			maxRow = Math.Min(rows, Exts.IntegerDivisionRoundingAwayFromZero(bounds.Bottom, binSize));
			maxCol = Math.Min(cols, Exts.IntegerDivisionRoundingAwayFromZero(bounds.Right, binSize));
		}

		void MutateBins(T actor, Rectangle bounds, Action<Dictionary<T, Rectangle>, T, Rectangle> action)
		{
			int minRow, maxRow, minCol, maxCol;
			BoundsToBinRowsAndCols(bounds, out minRow, out maxRow, out minCol, out maxCol);

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
			int minRow, maxRow, minCol, maxCol;
			BoundsToBinRowsAndCols(box, out minRow, out maxRow, out minCol, out maxCol);

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
	}
}
