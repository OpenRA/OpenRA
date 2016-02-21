#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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
		readonly Rectangle bounds;
		public readonly MapGridType GridType;
		public event Action<CPos> CellEntryChanged = null;

		readonly T[] entries;

		public CellLayer(Map map)
			: this(map.Grid.Type, new Size(map.MapSize.X, map.MapSize.Y)) { }

		public CellLayer(MapGridType gridType, Size size)
		{
			Size = size;
			bounds = new Rectangle(0, 0, Size.Width, Size.Height);
			GridType = gridType;
			entries = new T[size.Width * size.Height];
		}

		public void CopyValuesFrom(CellLayer<T> anotherLayer)
		{
			if (Size != anotherLayer.Size || GridType != anotherLayer.GridType)
				throw new ArgumentException(
					"layers must have a matching size and shape (grid type).", "anotherLayer");
			if (CellEntryChanged != null)
				throw new InvalidOperationException(
					"Cannot copy values when there are listeners attached to the CellEntryChanged event.");
			Array.Copy(anotherLayer.entries, entries, entries.Length);
		}

		public static CellLayer<T> CreateInstance(Func<MPos, T> initialCellValueFactory, Size size, MapGridType mapGridType)
		{
			var cellLayer = new CellLayer<T>(mapGridType, size);
			for (var v = 0; v < size.Height; v++)
			{
				for (var u = 0; u < size.Width; u++)
				{
					var mpos = new MPos(u, v);
					cellLayer[mpos] = initialCellValueFactory(mpos);
				}
			}

			return cellLayer;
		}

		// Resolve an array index from cell coordinates
		int Index(CPos cell)
		{
			return Index(cell.ToMPos(GridType));
		}

		// Resolve an array index from map coordinates
		int Index(MPos uv)
		{
			return uv.V * Size.Width + uv.U;
		}

		/// <summary>Gets or sets the <see cref="CellLayer"/> using cell coordinates</summary>
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
					CellEntryChanged(uv.ToCPos(GridType));
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
			return ((IEnumerable<T>)entries).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool Contains(CPos cell)
		{
			// .ToMPos() returns the same result if the X and Y coordinates
			// are switched. X < Y is invalid in the RectangularIsometric coordinate system,
			// so we pre-filter these to avoid returning the wrong result
			if (GridType == MapGridType.RectangularIsometric && cell.X < cell.Y)
				return false;

			return Contains(cell.ToMPos(GridType));
		}

		public bool Contains(MPos uv)
		{
			return bounds.Contains(uv.U, uv.V);
		}

		public CPos Clamp(CPos uv)
		{
			return Clamp(uv.ToMPos(GridType)).ToCPos(GridType);
		}

		public MPos Clamp(MPos uv)
		{
			return uv.Clamp(new Rectangle(0, 0, Size.Width - 1, Size.Height - 1));
		}
	}

	// Helper functions
	public static class CellLayer
	{
		/// <summary>Create a new layer by resizing another layer. New cells are filled with defaultValue.</summary>
		public static CellLayer<T> Resize<T>(CellLayer<T> layer, Size newSize, T defaultValue)
		{
			var result = new CellLayer<T>(layer.GridType, newSize);
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
