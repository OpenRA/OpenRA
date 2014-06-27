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
using OpenRA.Graphics;

namespace OpenRA
{
	// Represents a layer of "something" that covers the map
	public class CellLayer<T> : IEnumerable<T>
	{
		public readonly Size Size;
		T[] entries;

		public CellLayer(Map map)
			: this(new Size(map.MapSize.X, map.MapSize.Y)) { }

		public CellLayer(Size size)
		{
			Size = size;
			entries = new T[size.Width * size.Height];
		}

		// Resolve an array index from cell coordinates
		int Index(CPos cell)
		{
			// This will eventually define a distinct case for diagonal cell grids
			return cell.Y * Size.Width + cell.X;
		}

		/// <summary>Gets or sets the <see cref="OpenRA.CellLayer"/> using cell coordinates</summary>
		public T this[CPos cell]
		{
			get
			{
				return entries[Index(cell)];
			}

			set
			{
				entries[Index(cell)] = value;
			}
		}

		/// <summary>Gets or sets the layer contents using raw map coordinates (not CPos!)</summary>
		public T this[int u, int v]
		{
			get
			{
				return entries[v * Size.Width + u];
			}

			set
			{
				entries[v * Size.Width + u] = value;
			}
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
			var result = new CellLayer<T>(newSize);
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
