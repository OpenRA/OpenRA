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

using OpenRA.Primitives;

namespace OpenRA
{
	public sealed class ProjectedCellLayer<T> : CellLayerBase<T>
	{
		public ProjectedCellLayer(Map map)
			: base(map) { }

		public ProjectedCellLayer(MapGridType gridType, Size size)
			: base(gridType, size) { }

		// Resolve an array index from map coordinates.
		public int Index(PPos uv)
		{
			return uv.V * Size.Width + uv.U;
		}

		public T this[int index]
		{
			get
			{
				return entries[index];
			}

			set
			{
				entries[index] = value;
			}
		}

		/// <summary>Gets or sets the layer contents using projected map coordinates.</summary>
		public T this[PPos uv]
		{
			get
			{
				return entries[Index(uv)];
			}

			set
			{
				entries[Index(uv)] = value;
			}
		}

		public bool Contains(PPos uv)
		{
			return bounds.Contains(uv.U, uv.V);
		}
	}
}
