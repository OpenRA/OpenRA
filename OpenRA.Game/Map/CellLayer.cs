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
		public event Action<CPos> CellEntryChanged = null;

		readonly T[] entries;

		public CellLayer(Map map)
			: this(map.TileShape, new Size(map.MapSize.X, map.MapSize.Y)) { }

		public CellLayer(TileShape shape, Size size)
		{
			Size = size;
			Shape = shape;
			entries = new T[size.Width * size.Height];
		}

		public void CopyValuesFrom(CellLayer<T> anotherLayer)
		{
			if (Size != anotherLayer.Size || Shape != anotherLayer.Shape)
				throw new ArgumentException(
					"layers must have a matching size and shape.", "anotherLayer");
			if (CellEntryChanged != null)
				throw new InvalidOperationException(
					"Cannot copy values when there are listeners attached to the CellEntryChanged event.");
			Array.Copy(anotherLayer.entries, entries, entries.Length);
		}

		// Resolve an array index from cell coordinates
		int Index(CPos cell)
		{
			return Index(cell.ToMPos(Shape));
		}

		// Resolve an array index from map coordinates
		int Index(MPos uv)
		{
			return uv.V * Size.Width + uv.U;
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

				if (CellEntryChanged != null)
					CellEntryChanged(cell);
			}
		}

		/// <summary>Gets or sets the layer contents using raw map coordinates (not CPos!)</summary>
		public T this[MPos uv]
		{
			get
			{
				return entries[Index(uv)];
			}

			set
			{
				entries[Index(uv)] = value;

				if (CellEntryChanged != null)
					CellEntryChanged(uv.ToCPos(Shape));
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
			var result = new CellLayer<T>(layer.Shape, newSize);
			var width = Math.Min(layer.Size.Width, newSize.Width);
			var height = Math.Min(layer.Size.Height, newSize.Height);

			result.Clear(defaultValue);
			for (var j = 0; j < height; j++)
				for (var i = 0; i < width; i++)
					result[new MPos(i, j)] = layer[new MPos(i, j)];

			return result;
		}
	}
}
