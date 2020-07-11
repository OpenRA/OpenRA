#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Primitives;

namespace OpenRA
{
	// Represents a layer of "something" that covers the map
	public sealed class CellLayer<T> : CellLayerBase<T>
	{
		public event Action<CPos> CellEntryChanged = null;

		public CellLayer(Map map)
			: base(map) { }

		public CellLayer(MapGridType gridType, Size size)
			: base(gridType, size) { }

		public override void CopyValuesFrom(CellLayerBase<T> anotherLayer)
		{
			if (CellEntryChanged != null)
				throw new InvalidOperationException(
					"Cannot copy values when there are listeners attached to the CellEntryChanged event.");

			base.CopyValuesFrom(anotherLayer);
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

		public IEnumerable<T> Region(Map map)
		{
			var btl = new PPos(map.Bounds.Left, map.Bounds.Top);
			var bbr = new PPos(map.Bounds.Right - 1, map.Bounds.Bottom - 1);

			// PERF: Direct access to cell entries. Avoiding index calculation and property item lookup.
			if (GridType == MapGridType.Rectangular)
			{
				// PERF: Skip PPos to MPos conversion when possible
				var mapCoordsRegion = new MapCoordsRegion(
					(MPos)btl,
					(MPos)bbr);
				return new RectangularRegionCellEnumerable(this, mapCoordsRegion);
			}
			else
			{
				var projectedCells = new ProjectedCellRegion(map, btl, bbr);
				return new RegionCellEnumerable(this, projectedCells);
			}
		}

		class RectangularRegionCellEnumerable : IEnumerable<T>
		{
			readonly CellLayer<T> layer;
			readonly MapCoordsRegion region;

			public RectangularRegionCellEnumerable(CellLayer<T> layer, MapCoordsRegion region)
			{
				this.layer = layer;
				this.region = region;
			}

			public IEnumerator<T> GetEnumerator() { return new RectangularRegionCellEnumerator(layer, region); }
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}

		class RectangularRegionCellEnumerator : IEnumerator<T>
		{
			readonly CellLayer<T> layer;
			readonly MapCoordsRegion region;
			readonly int layerWidth;
			readonly int regionWidth;
			readonly int end;
			int rowStart;
			int rowEnd;
			int current;

			public RectangularRegionCellEnumerator(CellLayer<T> layer, MapCoordsRegion region)
			{
				this.layer = layer;
				this.region = region;
				layerWidth = layer.Size.Width;
				regionWidth = region.BottomRight.U - region.TopLeft.U;
				end = (region.BottomRight.V * layerWidth) + region.BottomRight.U;
				Reset();
			}

			public void Reset()
			{
				rowStart = (region.TopLeft.V * layerWidth) + region.TopLeft.U;
				rowEnd = rowStart + regionWidth;
				current = rowStart - 1;
			}

			public bool MoveNext()
			{
				if (++current > rowEnd)
				{
					rowStart += layerWidth;
					current = rowStart;
					if (current > end)
						return false;
					rowEnd = current + regionWidth;
				}

				return true;
			}

			public T Current { get { return layer.entries[current]; } }
			object System.Collections.IEnumerator.Current { get { return Current; } }
			public void Dispose() { }
		}

		class RegionCellEnumerable : IEnumerable<T>
		{
			readonly CellLayer<T> layer;
			readonly ProjectedCellRegion region;

			public RegionCellEnumerable(CellLayer<T> layer, ProjectedCellRegion region)
			{
				this.layer = layer;
				this.region = region;
			}

			public IEnumerator<T> GetEnumerator() { return new RegionCellEnumerator(layer, region); }
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}

		// Similar to ProjectedCellRegion enumerator
		class RegionCellEnumerator : IEnumerator<T>
		{
			readonly CellLayer<T> layer;
			readonly ProjectedCellRegion r;

			// Current position, in projected map coordinates
			int u, v;

			PPos current;

			public RegionCellEnumerator(CellLayer<T> layer, ProjectedCellRegion region)
			{
				this.layer = layer;
				r = region;
				Reset();
			}

			public bool MoveNext()
			{
				u += 1;

				// Check for column overflow
				if (u > r.BottomRight.U)
				{
					v += 1;
					u = r.TopLeft.U;

					// Check for row overflow
					if (v > r.BottomRight.V)
						return false;
				}

				current = new PPos(u, v);
				return true;
			}

			public void Reset()
			{
				// Enumerator starts *before* the first element in the sequence.
				u = r.TopLeft.U - 1;
				v = r.TopLeft.V;
			}

			public T Current { get { return layer[(MPos)current]; } }
			object System.Collections.IEnumerator.Current { get { return Current; } }
			public void Dispose() { }
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
