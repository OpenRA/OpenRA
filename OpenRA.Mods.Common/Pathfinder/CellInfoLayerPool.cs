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
using System.Collections.Generic;
using System.Drawing;

namespace OpenRA.Mods.Common.Pathfinder
{
	sealed class CellInfoLayerPool
	{
		const int MaxPoolSize = 4;
		readonly Stack<CellLayer<CellInfo>> pool = new Stack<CellLayer<CellInfo>>(MaxPoolSize);
		readonly CellLayer<CellInfo> defaultLayer;

		public CellInfoLayerPool(Map map)
		{
			defaultLayer =
				CellLayer<CellInfo>.CreateInstance(
					mpos => new CellInfo(int.MaxValue, int.MaxValue, mpos.ToCPos(map), CellStatus.Unvisited),
					new Size(map.MapSize.X, map.MapSize.Y),
					map.Grid.Type);
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

			if (layer == null)
				layer = new CellLayer<CellInfo>(defaultLayer.GridType, defaultLayer.Size);
			layer.CopyValuesFrom(defaultLayer);
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
			public CellLayer<CellInfo> Layer { get; private set; }
			CellInfoLayerPool layerPool;

			public PooledCellInfoLayer(CellInfoLayerPool layerPool)
			{
				this.layerPool = layerPool;
				Layer = layerPool.GetLayer();
			}

			public void Dispose()
			{
				if (Layer == null)
					return;
				layerPool.ReturnLayer(Layer);
				Layer = null;
				layerPool = null;
			}
		}
	}
}