#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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

		private PathSearch(IGraph<CellInfo> graph)
			: base(graph)
		{
		}

		public static IPathSearch Search(World world, Locomotor locomotor, Actor self, BlockedByActor check, Func<CPos, bool> goalCondition)
		{
			var graph = new PathGraph(LayerPoolForWorld(world), locomotor, self, world, check);
			var search = new PathSearch(graph);
			search.isGoal = goalCondition;
			search.heuristic = loc => 0;
			return search;
		}

		public static IPathSearch FromPoint(World world, Locomotor locomotor, Actor self, CPos @from, CPos target, BlockedByActor check)
		{
			return FromPoints(world, locomotor, self, new[] { from }, target, check);
		}

		public static IPathSearch FromPoints(World world, Locomotor locomotor, Actor self, IEnumerable<CPos> froms, CPos target, BlockedByActor check)
		{
			var graph = new PathGraph(LayerPoolForWorld(world), locomotor, self, world, check);
			var search = new PathSearch(graph);
			search.heuristic = search.DefaultEstimator(target);

			// The search will aim for the shortest path by default, a weight of 100%.
			// We can allow the search to find paths that aren't optimal by changing the weight.
			// We provide a weight that limits the worst case length of the path,
			// e.g. a weight of 110% will find a path no more than 10% longer than the shortest possible.
			// The benefit of allowing the search to return suboptimal paths is faster computation time.
			// The search can skip some areas of the search space, meaning it has less work to do.
			// We allow paths up to 25% longer than the shortest, optimal path, to improve pathfinding time.
			search.heuristicWeightPercentage = 125;

			search.isGoal = loc =>
			{
				var locInfo = search.Graph[loc];
				return locInfo.EstimatedTotalCost - locInfo.CostSoFar == 0;
			};

			foreach (var sl in froms)
				if (world.Map.Contains(sl))
					search.AddInitialCell(sl);

			return search;
		}

		protected override void AddInitialCell(CPos location)
		{
			var cost = heuristic(location);
			Graph[location] = new CellInfo(CellStatus.Open, 0, cost, location);
			var connection = new GraphConnection(location, cost);
			OpenQueue.Add(connection);
			StartPoints.Add(connection);
		}

		/// <summary>
		/// This function analyzes the neighbors of the most promising node in the Pathfinding graph
		/// using the A* algorithm (A-star) and returns that node
		/// </summary>
		/// <returns>The most promising node of the iteration</returns>
		public override CPos Expand()
		{
			var currentMinNode = OpenQueue.Pop().Destination;

			var currentInfo = Graph[currentMinNode];
			Graph[currentMinNode] = new CellInfo(CellStatus.Closed, currentInfo.CostSoFar, currentInfo.EstimatedTotalCost, currentInfo.PreviousNode);

			if (Graph.CustomCost != null && Graph.CustomCost(currentMinNode) == PathGraph.PathCostForInvalidPath)
				return currentMinNode;

			foreach (var connection in Graph.GetConnections(currentMinNode))
			{
				// Calculate the cost up to that point
				var costSoFarToNeighbor = currentInfo.CostSoFar + connection.Cost;

				var neighbor = connection.Destination;
				var neighborInfo = Graph[neighbor];

				// Cost is even higher; next direction:
				if (neighborInfo.Status == CellStatus.Closed ||
					(neighborInfo.Status == CellStatus.Open && costSoFarToNeighbor >= neighborInfo.CostSoFar))
					continue;

				// Now we may seriously consider this direction using heuristics. If the cell has
				// already been processed, we can reuse the result (just the difference between the
				// estimated total and the cost so far)
				int estimatedRemainingCostToTarget;
				if (neighborInfo.Status == CellStatus.Open)
					estimatedRemainingCostToTarget = neighborInfo.EstimatedTotalCost - neighborInfo.CostSoFar;
				else
					estimatedRemainingCostToTarget = heuristic(neighbor);

				var estimatedTotalCostToTarget = costSoFarToNeighbor + estimatedRemainingCostToTarget;
				Graph[neighbor] = new CellInfo(CellStatus.Open, costSoFarToNeighbor, estimatedTotalCostToTarget, currentMinNode);

				if (neighborInfo.Status != CellStatus.Open)
					OpenQueue.Add(new GraphConnection(neighbor, estimatedTotalCostToTarget));
			}

			return currentMinNode;
		}
	}
}
