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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// A dense pathfinding graph that supports a search over all cells within a map.
	/// It implements the ability to cost and get connections for cells, and supports <see cref="ICustomMovementLayer"/>.
	/// </summary>
	sealed class MapPathGraph : DensePathGraph
	{
		readonly CellInfoLayerPool.PooledCellInfoLayer pooledLayer;
		readonly CellLayer<CellInfo>[] cellInfoForLayer;

		public MapPathGraph(CellInfoLayerPool layerPool, Locomotor locomotor, Actor actor, World world, BlockedByActor check,
			Func<CPos, int> customCost, Actor ignoreActor, bool laneBias, bool inReverse)
			: base(locomotor, actor, world, check, customCost, ignoreActor, laneBias, inReverse)
		{
			// As we support a search over the whole map area,
			// use the pool to grab the CellInfos we need to track the graph state.
			// This allows us to avoid the cost of allocating large arrays constantly.
			// PERF: Avoid LINQ
			pooledLayer = layerPool.Get();
			cellInfoForLayer = new CellLayer<CellInfo>[CustomMovementLayers.Length];
			cellInfoForLayer[0] = pooledLayer.GetLayer();
			foreach (var cml in CustomMovementLayers)
				if (cml != null && cml.EnabledForLocomotor(locomotor.Info))
					cellInfoForLayer[cml.Index] = pooledLayer.GetLayer();
		}

		public override CellInfo this[CPos pos]
		{
			get => cellInfoForLayer[pos.Layer][pos];
			set => cellInfoForLayer[pos.Layer][pos] = value;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				pooledLayer.Dispose();

			base.Dispose(disposing);
		}
	}
}
