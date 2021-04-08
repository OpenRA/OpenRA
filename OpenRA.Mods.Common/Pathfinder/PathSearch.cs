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
using System.Runtime.CompilerServices;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	public sealed class PathSearch : BasePathSearch
	{
		// PERF: Maintain a pool of layers used for paths searches for each world. These searches are performed often
		// so we wish to avoid the high cost of initializing a new search space every time by reusing the old ones.
		static readonly ConditionalWeakTable<World, CellInfoLayerPool> LayerPoolTable = new ConditionalWeakTable<World, CellInfoLayerPool>();
		static readonly ConditionalWeakTable<World, CellInfoLayerPool>.CreateValueCallback CreateLayerPool = world => new CellInfoLayerPool(world.Map);

		static CellInfoLayerPool LayerPoolForWorld(World world)
		{
			return LayerPoolTable.GetValue(world, CreateLayerPool);
		}

		public override IEnumerable<(CPos, int)> Considered => considered;

		LinkedList<(CPos, int)> considered;

		#region Constructors

		public PathSearch(PathQuery query, bool debug = false)
			: base(new PathGraph(LayerPoolForWorld(query.World), query), query, debug)
		{
			considered = new LinkedList<(CPos, int)>();
			if (Query.FromPositions != null)
			{
				var map = Query.World.Map;
				foreach (var sl in Query.FromPositions)
					if (map.Contains(sl))
						AddInitialCell(sl);
			}
		}

		void AddInitialCell(CPos location)
		{
			var cost = heuristic(location);
			Graph[location] = new CellInfo(0, cost, location, CellStatus.Open);
			var connection = new GraphConnection(location, cost);
			OpenQueue.Add(connection);
			considered.AddLast((location, 0));
		}

		#endregion

		/// <summary>
		/// This function analyzes the neighbors of the most promising node in the Pathfinding graph
		/// using the A* algorithm (A-star) and returns that node
		/// </summary>
		/// <returns>The most promising node of the iteration or false if expansion is no longer possible</returns>
		public override bool TryExpand(out CPos mostPromisingNode)
		{
			if (!OpenQueue.TryPop(out var currentMinConnection))
			{
				mostPromisingNode = default(CPos);
				return false;
			}

			var currentMinNode = currentMinConnection.Destination;

			var currentCell = Graph[currentMinNode];
			Graph[currentMinNode] = new CellInfo(currentCell.CostSoFar,
				currentCell.EstimatedTotal, currentCell.PreviousPos, CellStatus.Closed);

			var customCost = Query.CustomCost;
			if (customCost != null && customCost(currentMinNode) == PathGraph.CostForInvalidCell)
			{
				mostPromisingNode = currentMinNode;
				return true;
			}

			foreach (var connection in Graph.GetConnections(currentMinNode))
			{
				// Calculate the cost up to that point
				var gCost = currentCell.CostSoFar + connection.Cost;

				var neighborCPos = connection.Destination;
				var neighborCell = Graph[neighborCPos];

				// Cost is even higher; next direction:
				if (neighborCell.Status == CellStatus.Closed || gCost >= neighborCell.CostSoFar)
					continue;

				// Now we may seriously consider this direction using heuristics. If the cell has
				// already been processed, we can reuse the result (just the difference between the
				// estimated total and the cost so far
				int hCost;
				if (neighborCell.Status == CellStatus.Open)
					hCost = neighborCell.EstimatedTotal - neighborCell.CostSoFar;
				else
					hCost = heuristic(neighborCPos);

				var estimatedCost = gCost + hCost;
				Graph[neighborCPos] = new CellInfo(gCost, estimatedCost, currentMinNode, CellStatus.Open);

				if (neighborCell.Status != CellStatus.Open)
					OpenQueue.Add(new GraphConnection(neighborCPos, estimatedCost));

				if (Debug)
				{
					if (gCost > MaxCost)
						MaxCost = gCost;

					considered.AddLast((neighborCPos, gCost));
				}
			}

			mostPromisingNode = currentMinNode;
			return true;
		}
	}
}
