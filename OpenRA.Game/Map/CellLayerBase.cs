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
using System.Collections;
using System.Collections.Generic;
using OpenRA.Primitives;

namespace OpenRA
{
	public abstract class CellLayerBase<T> : IEnumerable<T>
	{
		public readonly Size Size;
		public readonly MapGridType GridType;

		protected readonly T[] entries;
		protected readonly Rectangle bounds;

		public CellLayerBase(Map map)
			: this(map.Grid.Type, new Size(map.MapSize.X, map.MapSize.Y)) { }

		public CellLayerBase(MapGridType gridType, Size size)
		{
			Size = size;
			bounds = new Rectangle(0, 0, Size.Width, Size.Height);
			GridType = gridType;
			entries = new T[size.Width * size.Height];
		}

		public virtual void CopyValuesFrom(CellLayerBase<T> anotherLayer)
		{
			if (Size != anotherLayer.Size || GridType != anotherLayer.GridType)
				throw new ArgumentException("Layers must have a matching size and shape (grid type).", "anotherLayer");

			Array.Copy(anotherLayer.entries, entries, entries.Length);
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
	}
}
