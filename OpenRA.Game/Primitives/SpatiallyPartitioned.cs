#region Copyright & License Information
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
			rows = height / binSize + 1;
			cols = width / binSize + 1;
			itemBoundsBins = Exts.MakeArray(rows * cols, _ => new Dictionary<T, Rectangle>());
		}

		public void Add(T item, Rectangle bounds)
		{
			itemBounds.Add(item, bounds);
			MutateBins(item, bounds, addItem);
		}

		public void Update(T item, Rectangle bounds)
		{
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

		void MutateBins(T actor, Rectangle bounds, Action<Dictionary<T, Rectangle>, T, Rectangle> action)
		{
			var top = Math.Max(0, bounds.Top / binSize);
			var left = Math.Max(0, bounds.Left / binSize);
			var bottom = Math.Min(rows - 1, bounds.Bottom / binSize);
			var right = Math.Min(cols - 1, bounds.Right / binSize);

			for (var row = top; row <= bottom; row++)
				for (var col = left; col <= right; col++)
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
			var left = (box.Left / binSize).Clamp(0, cols - 1);
			var right = (box.Right / binSize).Clamp(0, cols - 1);
			var top = (box.Top / binSize).Clamp(0, rows - 1);
			var bottom = (box.Bottom / binSize).Clamp(0, rows - 1);

			var items = new HashSet<T>();
			for (var row = top; row <= bottom; row++)
				for (var col = left; col <= right; col++)
				{
					var binBounds = BinBounds(row, col);
					foreach (var kvp in BinAt(row, col))
					{
						var item = kvp.Key;
						var bounds = kvp.Value;

						// Return items that intersect the box. We also want to avoid returning the same item many times.
						// If the item is contained wholly within this bin, we're good as we know it won't show up in any others.
						// Otherwise it may appear in another bin. We use a set of seen items to avoid yielding it again.
						if (bounds.IntersectsWith(box) && (binBounds.Contains(bounds) || items.Add(item)))
							yield return item;
					}
				}
		}
	}
}
