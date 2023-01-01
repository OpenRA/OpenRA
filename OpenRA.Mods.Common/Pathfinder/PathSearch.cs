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
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Pathfinder
{
	public sealed class PathSearch : IDisposable
	{
		public interface IRecorder
		{
			void Add(CPos source, CPos destination, int costSoFar, int estimatedRemainingCost);
		}

		// PERF: Maintain a pool of layers used for paths searches for each world. These searches are performed often
		// so we wish to avoid the high cost of initializing a new search space every time by reusing the old ones.
		static readonly ConditionalWeakTable<World, CellInfoLayerPool> LayerPoolTable = new ConditionalWeakTable<World, CellInfoLayerPool>();
		static readonly ConditionalWeakTable<World, CellInfoLayerPool>.CreateValueCallback CreateLayerPool = world => new CellInfoLayerPool(world.Map);

		static CellInfoLayerPool LayerPoolForWorld(World world)
		{
			return LayerPoolTable.GetValue(world, CreateLayerPool);
		}

		public static PathSearch ToTargetCellByPredicate(
			World world, Locomotor locomotor, Actor self,
			IEnumerable<CPos> froms, Func<CPos, bool> targetPredicate, BlockedByActor check,
			Func<CPos, int> customCost = null,
			Actor ignoreActor = null,
			bool laneBias = true,
			IRecorder recorder = null)
		{
			var graph = new MapPathGraph(LayerPoolForWorld(world), locomotor, self, world, check, customCost, ignoreActor, laneBias, false);
			var search = new PathSearch(graph, loc => 0, 0, targetPredicate, recorder);

			AddInitialCells(world, locomotor, froms, customCost, search);

			return search;
		}

		public static PathSearch ToTargetCell(
			World world, Locomotor locomotor, Actor self,
			IEnumerable<CPos> froms, CPos target, BlockedByActor check, int heuristicWeightPercentage,
			Func<CPos, int> customCost = null,
			Actor ignoreActor = null,
			bool laneBias = true,
			bool inReverse = false,
			Func<CPos, int> heuristic = null,
			Grid? grid = null,
			IRecorder recorder = null)
		{
			IPathGraph graph;
			if (grid != null)
				graph = new GridPathGraph(locomotor, self, world, check, customCost, ignoreActor, laneBias, inReverse, grid.Value);
			else
				graph = new MapPathGraph(LayerPoolForWorld(world), locomotor, self, world, check, customCost, ignoreActor, laneBias, inReverse);

			heuristic = heuristic ?? DefaultCostEstimator(locomotor, target);
			var search = new PathSearch(graph, heuristic, heuristicWeightPercentage, loc => loc == target, recorder);

			AddInitialCells(world, locomotor, froms, customCost, search);

			return search;
		}

		public static bool CellAllowsMovement(World world, Locomotor locomotor, CPos cell, Func<CPos, int> customCost)
		{
			return world.Map.Contains(cell) &&
				(cell.Layer == 0 || world.GetCustomMovementLayers()[cell.Layer].EnabledForLocomotor(locomotor.Info)) &&
				(customCost == null || customCost(cell) != PathGraph.PathCostForInvalidPath);
		}

		static void AddInitialCells(World world, Locomotor locomotor, IEnumerable<CPos> froms, Func<CPos, int> customCost, PathSearch search)
		{
			foreach (var sl in froms)
				if (CellAllowsMovement(world, locomotor, sl, customCost))
					search.AddInitialCell(sl, customCost);
		}

		public static PathSearch ToTargetCellOverGraph(
			Func<CPos, List<GraphConnection>> edges, Locomotor locomotor, CPos from, CPos target,
			int estimatedSearchSize = 0, IRecorder recorder = null)
		{
			var graph = new SparsePathGraph(edges, estimatedSearchSize);
			var search = new PathSearch(graph, DefaultCostEstimator(locomotor, target), 100, loc => loc == target, recorder);

			search.AddInitialCell(from, null);

			return search;
		}

		/// <summary>
		/// Default: Diagonal distance heuristic. More information:
		/// http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html
		/// Layers are ignored and incur no additional cost.
		/// </summary>
		/// <param name="locomotor">Locomotor used to provide terrain costs.</param>
		/// <param name="destination">The cell for which costs are to be given by the estimation function.</param>
		/// <returns>A delegate that calculates the cost estimation between the <paramref name="destination"/> and the given cell.</returns>
		public static Func<CPos, int> DefaultCostEstimator(Locomotor locomotor, CPos destination)
		{
			var estimator = DefaultCostEstimator(locomotor);
			return here => estimator(here, destination);
		}

		/// <summary>
		/// Default: Diagonal distance heuristic. More information:
		/// http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html
		/// Layers are ignored and incur no additional cost.
		/// </summary>
		/// <param name="locomotor">Locomotor used to provide terrain costs.</param>
		/// <returns>A delegate that calculates the cost estimation between the given cells.</returns>
		public static Func<CPos, CPos, int> DefaultCostEstimator(Locomotor locomotor)
		{
			// Determine the minimum possible cost for moving horizontally between cells based on terrain speeds.
			// The minimum possible cost diagonally is then Sqrt(2) times more costly.
			var cellCost = locomotor.Info.TerrainSpeeds.Values.Min(ti => ti.Cost);
			var diagonalCellCost = Exts.MultiplyBySqrtTwo(cellCost);
			return (here, destination) =>
			{
				var diag = Math.Min(Math.Abs(here.X - destination.X), Math.Abs(here.Y - destination.Y));
				var straight = Math.Abs(here.X - destination.X) + Math.Abs(here.Y - destination.Y);

				// According to the information link, this is the shape of the function.
				// We just extract factors to simplify.
				// Possible simplification: var h = Constants.CellCost * (straight + (Constants.Sqrt2 - 2) * diag);
				return cellCost * straight + (diagonalCellCost - 2 * cellCost) * diag;
			};
		}

		public IPathGraph Graph { get; }
		public Func<CPos, bool> TargetPredicate { get; set; }
		readonly Func<CPos, int> heuristic;
		readonly int heuristicWeightPercentage;
		readonly IRecorder recorder;
		readonly IPriorityQueue<GraphConnection> openQueue;

		/// <summary>
		/// Initialize a new search.
		/// </summary>
		/// <param name="graph">Graph over which the search is conducted.</param>
		/// <param name="heuristic">Provides an estimation of the distance between the given cell and the target.</param>
		/// <param name="heuristicWeightPercentage">
		/// The search will aim for the shortest path when given a weight of 100%.
		/// We can allow the search to find paths that aren't optimal by changing the weight.
		/// The weight limits the worst case length of the path,
		/// e.g. a weight of 110% will find a path no more than 10% longer than the shortest possible.
		/// The benefit of allowing the search to return suboptimal paths is faster computation time.
		/// The search can skip some areas of the search space, meaning it has less work to do.
		/// </param>
		/// <param name="targetPredicate">Determines if the given cell is the target.</param>
		PathSearch(IPathGraph graph, Func<CPos, int> heuristic, int heuristicWeightPercentage, Func<CPos, bool> targetPredicate, IRecorder recorder)
		{
			Graph = graph;
			this.heuristic = heuristic;
			this.heuristicWeightPercentage = heuristicWeightPercentage;
			TargetPredicate = targetPredicate;
			this.recorder = recorder;
			openQueue = new PriorityQueue<GraphConnection>(GraphConnection.ConnectionCostComparer);
		}

		void AddInitialCell(CPos location, Func<CPos, int> customCost)
		{
			var initialCost = 0;
			if (customCost != null)
			{
				initialCost = customCost(location);
				if (initialCost == PathGraph.PathCostForInvalidPath)
					return;
			}

			var heuristicCost = heuristic(location);
			if (heuristicCost == PathGraph.PathCostForInvalidPath)
				return;

			var estimatedCost = heuristicCost * heuristicWeightPercentage / 100;
			Graph[location] = new CellInfo(CellStatus.Open, initialCost, initialCost + estimatedCost, location);
			var connection = new GraphConnection(location, estimatedCost);
			openQueue.Add(connection);
		}

		/// <summary>
		/// Determines if there are more reachable cells and the search can be continued.
		/// If false, <see cref="Expand"/> can no longer be called.
		/// </summary>
		bool CanExpand()
		{
			// Connections to a cell can appear more than once if a search discovers a lower cost route to the cell.
			// The lower cost gets processed first and the cell will be Closed.
			// When checking if we can expand, pop any Closed cells off the queue, so Expand will only see Open cells.
			CellStatus status;
			do
			{
				if (openQueue.Empty)
					return false;

				status = Graph[openQueue.Peek().Destination].Status;
				if (status == CellStatus.Closed)
					openQueue.Pop();
			}
			while (status == CellStatus.Closed);

			return true;
		}

		/// <summary>
		/// This function analyzes the neighbors of the most promising node in the pathfinding graph
		/// using the A* algorithm (A-star) and returns that node
		/// </summary>
		/// <returns>The most promising node of the iteration</returns>
		CPos Expand()
		{
			var currentMinNode = openQueue.Pop().Destination;

			var currentInfo = Graph[currentMinNode];
			Graph[currentMinNode] = new CellInfo(CellStatus.Closed, currentInfo.CostSoFar, currentInfo.EstimatedTotalCost, currentInfo.PreviousNode);

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

				// Now we may seriously consider this direction using heuristics.
				int estimatedRemainingCostToTarget;
				if (neighborInfo.Status == CellStatus.Open)
				{
					// If the cell has already been processed, we can reuse the result
					// (just the difference between the estimated total and the cost so far)
					estimatedRemainingCostToTarget = neighborInfo.EstimatedTotalCost - neighborInfo.CostSoFar;
				}
				else
				{
					// If the heuristic reports the cell is unreachable, we won't consider it.
					var heuristicCost = heuristic(neighbor);
					if (heuristicCost == PathGraph.PathCostForInvalidPath)
						continue;
					estimatedRemainingCostToTarget = heuristicCost * heuristicWeightPercentage / 100;
				}

				recorder?.Add(currentMinNode, neighbor, costSoFarToNeighbor, estimatedRemainingCostToTarget);

				var estimatedTotalCostToTarget = costSoFarToNeighbor + estimatedRemainingCostToTarget;
				Graph[neighbor] = new CellInfo(CellStatus.Open, costSoFarToNeighbor, estimatedTotalCostToTarget, currentMinNode);
				openQueue.Add(new GraphConnection(neighbor, estimatedTotalCostToTarget));
			}

			return currentMinNode;
		}

		/// <summary>
		/// Expands the path search until a path is found, and returns whether a path is found successfully.
		/// </summary>
		/// <remarks>
		/// If the path search has previously been expanded it will only return true if a path can be found during
		/// *this* expansion of the search. If the search was expanded previously and the target is already
		/// <see cref="CellStatus.Closed"/> then this method will return false.
		/// </remarks>
		public bool ExpandToTarget()
		{
			while (CanExpand())
				if (TargetPredicate(Expand()))
					return true;

			return false;
		}

		/// <summary>
		/// Expands the path search over the whole search space.
		/// Returns the cells that were visited during the search.
		/// </summary>
		public List<CPos> ExpandAll()
		{
			var consideredCells = new List<CPos>();
			while (CanExpand())
				consideredCells.Add(Expand());
			return consideredCells;
		}

		/// <summary>
		/// Expands the path search until a path is found, and returns that path.
		/// Returned path is *reversed* and given target to source.
		/// </summary>
		public List<CPos> FindPath()
		{
			while (CanExpand())
			{
				var p = Expand();
				if (TargetPredicate(p))
					return MakePath(Graph, p);
			}

			return PathFinder.NoPath;
		}

		// Build the path from the destination.
		// When we find a node that has the same previous position than itself, that node is the source node.
		static List<CPos> MakePath(IPathGraph graph, CPos destination)
		{
			var ret = new List<CPos>();
			var currentNode = destination;

			while (graph[currentNode].PreviousNode != currentNode)
			{
				ret.Add(currentNode);
				currentNode = graph[currentNode].PreviousNode;
			}

			ret.Add(currentNode);
			return ret;
		}

		/// <summary>
		/// Expands both path searches until they intersect, and returns the path.
		/// Returned path is from the source of the first search to the source of the second search.
		/// </summary>
		public static List<CPos> FindBidiPath(PathSearch first, PathSearch second)
		{
			while (first.CanExpand() && second.CanExpand())
			{
				// make some progress on the first search
				var p = first.Expand();
				var pInfo = second.Graph[p];
				if (pInfo.Status == CellStatus.Closed &&
					pInfo.CostSoFar != PathGraph.PathCostForInvalidPath)
					return MakeBidiPath(first, second, p);

				// make some progress on the second search
				var q = second.Expand();
				var qInfo = first.Graph[q];
				if (qInfo.Status == CellStatus.Closed &&
					qInfo.CostSoFar != PathGraph.PathCostForInvalidPath)
					return MakeBidiPath(first, second, q);
			}

			return PathFinder.NoPath;
		}

		// Build the path from the destination of each search.
		// When we find a node that has the same previous position than itself, that is the source of that search.
		static List<CPos> MakeBidiPath(PathSearch first, PathSearch second, CPos confluenceNode)
		{
			var ca = first.Graph;
			var cb = second.Graph;

			var ret = new List<CPos>();

			var q = confluenceNode;
			var previous = ca[q].PreviousNode;
			while (previous != q)
			{
				ret.Add(q);
				q = previous;
				previous = ca[q].PreviousNode;
			}

			ret.Add(q);

			ret.Reverse();

			q = confluenceNode;
			previous = cb[q].PreviousNode;
			while (previous != q)
			{
				q = previous;
				previous = cb[q].PreviousNode;
				ret.Add(q);
			}

			return ret;
		}

		public void Dispose()
		{
			Graph.Dispose();
		}
	}
}
