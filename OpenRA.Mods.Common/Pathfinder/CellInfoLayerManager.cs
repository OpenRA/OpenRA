#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	public interface ICellInfoLayerManager
	{
		/// <summary>
		/// Gets a CellLayer of Nodes from the pool
		/// </summary>
		CellLayer<CellInfo> GetFromPool();

		/// <summary>
		/// Puts a CellLayer into the pool
		/// </summary>
		void PutBackIntoPool(CellLayer<CellInfo> ci);

		/// <summary>
		/// Creates (or obtains from the pool) a CellLayer given a map
		/// </summary>
		CellLayer<CellInfo> NewLayer(Map map);
	}

	public sealed class CellInfoLayerManager : ICellInfoLayerManager
	{
		readonly Queue<CellLayer<CellInfo>> cellInfoPool = new Queue<CellLayer<CellInfo>>();
		readonly object defaultCellInfoLayerSync = new object();
		CellLayer<CellInfo> defaultCellInfoLayer;

		static ICellInfoLayerManager instance = new CellInfoLayerManager();

		public static ICellInfoLayerManager Instance
		{
			get
			{
				return instance;
			}
		}

		public static void SetInstance(ICellInfoLayerManager cellInfoLayerManager)
		{
			instance = cellInfoLayerManager;
		}

		public CellLayer<CellInfo> GetFromPool()
		{
			lock (cellInfoPool)
				return cellInfoPool.Dequeue();
		}

		public void PutBackIntoPool(CellLayer<CellInfo> ci)
		{
			lock (cellInfoPool)
				cellInfoPool.Enqueue(ci);
		}

		public CellLayer<CellInfo> NewLayer(Map map)
		{
			CellLayer<CellInfo> result = null;
			var mapSize = new Size(map.MapSize.X, map.MapSize.Y);

			// HACK: Uses a static cache so that double-ended searches (which have two PathSearch instances)
			// can implicitly share data.  The PathFinder should allocate the CellInfo array and pass it
			// explicitly to the things that need to share it.
			while (cellInfoPool.Count > 0)
			{
				var cellInfo = GetFromPool();
				if (cellInfo.Size != mapSize || cellInfo.Shape != map.TileShape)
				{
					Log.Write("debug", "Discarding old pooled CellInfo of wrong size.");
					continue;
				}

				result = cellInfo;
				break;
			}

			if (result == null)
				result = new CellLayer<CellInfo>(map);

			lock (defaultCellInfoLayerSync)
			{
				if (defaultCellInfoLayer == null ||
					defaultCellInfoLayer.Size != mapSize ||
					defaultCellInfoLayer.Shape != map.TileShape)
				{
					defaultCellInfoLayer =
						CellLayer<CellInfo>.CreateInstance(
							mpos => new CellInfo(int.MaxValue, int.MaxValue, mpos.ToCPos(map), CellStatus.Unvisited),
							new Size(map.MapSize.X, map.MapSize.Y),
							map.TileShape);
				}

				result.CopyValuesFrom(defaultCellInfoLayer);
			}

			return result;
		}
	}
}