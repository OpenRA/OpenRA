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
using System.Collections.Generic;

namespace OpenRA.Mods.Common.Pathfinder
{
	sealed class CellInfoLayerPool
	{
		const int MaxPoolSize = 4;
		readonly Stack<CellLayer<CellInfo>> pool = new Stack<CellLayer<CellInfo>>(MaxPoolSize);
		readonly Map map;

		public CellInfoLayerPool(Map map)
		{
			this.map = map;
		}

		public PooledCellInfoLayer Get()
		{
			return new PooledCellInfoLayer(this);
		}

		CellLayer<CellInfo> GetLayer()
		{
			CellLayer<CellInfo> layer = null;
			lock (pool)
				if (pool.Count > 0)
					layer = pool.Pop();

			// As the default value of CellInfo represents an Unvisited location,
			// we don't need to initialize the values in the layer,
			// we can just clear them to the defaults.
			if (layer == null)
				layer = new CellLayer<CellInfo>(map);
			else
				layer.Clear();

			return layer;
		}

		void ReturnLayer(CellLayer<CellInfo> layer)
		{
			lock (pool)
			   if (pool.Count < MaxPoolSize)
					pool.Push(layer);
		}

		public class PooledCellInfoLayer : IDisposable
		{
			CellInfoLayerPool layerPool;
			List<CellLayer<CellInfo>> layers = new List<CellLayer<CellInfo>>();

			public PooledCellInfoLayer(CellInfoLayerPool layerPool)
			{
				this.layerPool = layerPool;
			}

			public CellLayer<CellInfo> GetLayer()
			{
				var layer = layerPool.GetLayer();
				layers.Add(layer);
				return layer;
			}

			public void Dispose()
			{
				if (layerPool != null)
					foreach (var layer in layers)
						layerPool.ReturnLayer(layer);

				layers = null;
				layerPool = null;
			}
		}
	}
}
