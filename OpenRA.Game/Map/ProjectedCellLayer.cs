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

using OpenRA.Primitives;

namespace OpenRA
{
	public sealed class ProjectedCellLayer<T> : CellLayerBase<T>
	{
		public int MaxIndex => Size.Width * Size.Height;

		public ProjectedCellLayer(Map map)
			: base(map) { }

		public ProjectedCellLayer(MapGridType gridType, Size size)
			: base(gridType, size) { }

		// Resolve an array index from map coordinates.
		public int Index(PPos uv)
		{
			return uv.V * Size.Width + uv.U;
		}

		public PPos PPosFromIndex(int index)
		{
			return new PPos(index % Size.Width, index / Size.Width);
		}

		public T this[int index]
		{
			get => Entries[index];

			set => Entries[index] = value;
		}

		/// <summary>Gets or sets the layer contents using projected map coordinates.</summary>
		public T this[PPos uv]
		{
			get => Entries[Index(uv)];

			set => Entries[Index(uv)] = value;
		}

		public bool Contains(PPos uv)
		{
			return Bounds.Contains(uv.U, uv.V);
		}
	}
}
