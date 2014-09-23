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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace OpenRA
{
	// Represents a layer of "something" that covers the map
	public class CellLayer<T> : IEnumerable<T>
	{
		public readonly Size Size;
		public readonly TileShape Shape;
		readonly T[] entries;

		public CellLayer(Map map)
			: this(map.TileShape, new Size(map.MapSize.X, map.MapSize.Y)) { }

		public CellLayer(TileShape shape, Size size)
		{
			Size = size;
			Shape = shape;
			entries = new T[size.Width * size.Height];
		}

		// Resolve an array index from cell coordinates
		int Index(CPos cell)
		{
			var uv = Map.CellToMap(Shape, cell);
			return Index(uv.X, uv.Y);
		}

		// Resolve an array index from map coordinates
		int Index(int u, int v)
		{
			return v * Size.Width + u;
		}

		/// <summary>Gets or sets the <see cref="OpenRA.CellLayer"/> using cell coordinates</summary>
		public T this[CPos cell]
		{
			get
			{
				if (Index(cell) < (entries.Length - 1))
					return entries[Index(cell)];
				else
					return entries[(entries.Length - 1)];
			}
			set
			{
				if (Index(cell) < (entries.Length - 1))
					entries[Index(cell)] = value;
				else
					entries[(entries.Length - 1)] = value;
			}
		}

		/// <summary>Gets or sets the layer contents using raw map coordinates (not CPos!)</summary>
		public T this[int u, int v]
		{
			get { return entries[Index(u, v)]; }
			set { entries[Index(u, v)] = value; }
		}

		/// <summary>Clears the layer contents with a known value</summary>
		public void Clear(T clearValue)
		{
			for (var i = 0; i < entries.Length; i++)
				entries[i] = clearValue;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return (IEnumerator<T>)entries.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	// Helper functions
	public static class CellLayer
	{
		/// <summary>Create a new layer by resizing another layer.  New cells are filled with defaultValue.</summary>
		public static CellLayer<T> Resize<T>(CellLayer<T> layer, Size newSize, T defaultValue)
		{
			var result = new CellLayer<T>(layer.Shape, newSize);
			var width = Math.Min(layer.Size.Width, newSize.Width);
			var height = Math.Min(layer.Size.Height, newSize.Height);

			result.Clear(defaultValue);
			for (var j = 0; j < height; j++)
				for (var i = 0; i < width; i++)
					result[i, j] = layer[i, j];

			return result;
		}
	}
}
