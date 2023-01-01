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

		protected readonly T[] Entries;
		protected readonly Rectangle Bounds;

		public CellLayerBase(Map map)
			: this(map.Grid.Type, new Size(map.MapSize.X, map.MapSize.Y)) { }

		public CellLayerBase(MapGridType gridType, Size size)
		{
			Size = size;
			Bounds = new Rectangle(0, 0, Size.Width, Size.Height);
			GridType = gridType;
			Entries = new T[size.Width * size.Height];
		}

		public virtual void CopyValuesFrom(CellLayerBase<T> anotherLayer)
		{
			if (Size != anotherLayer.Size || GridType != anotherLayer.GridType)
				throw new ArgumentException("Layers must have a matching size and shape (grid type).", nameof(anotherLayer));

			Array.Copy(anotherLayer.Entries, Entries, Entries.Length);
		}

		/// <summary>Clears the layer contents with their default value</summary>
		public virtual void Clear()
		{
			Array.Clear(Entries, 0, Entries.Length);
		}

		/// <summary>Clears the layer contents with a known value</summary>
		public virtual void Clear(T clearValue)
		{
			Array.Fill(Entries, clearValue);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return ((IEnumerable<T>)Entries).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
