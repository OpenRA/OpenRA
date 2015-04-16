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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Pathfinder
{
	public sealed class PathSearch : BasePathSearch
	{
		public override IEnumerable<Pair<CPos, int>> Considered
		{
			get { return considered; }
		}

		LinkedList<Pair<CPos, int>> considered;

		#region Constructors

		private PathSearch(IGraph<CellInfo> graph)
			: base(graph)
		{
			considered = new LinkedList<Pair<CPos, int>>();
		}

		public static IPathSearch Search(IWorld world, IMobileInfo mi, IActor self, bool checkForBlocked)
		{
			var graph = new PathGraph(CellInfoLayerManager.Instance.NewLayer(world.Map), mi, self, world, checkForBlocked);
			return new PathSearch(graph);
		}

		public static IPathSearch FromPoint(IWorld world, IMobileInfo mi, IActor self, CPos from, CPos target, bool checkForBlocked)
		{
			var graph = new PathGraph(CellInfoLayerManager.Instance.NewLayer(world.Map), mi, self, world, checkForBlocked);
			var search = new PathSearch(graph)
			{
				heuristic = DefaultEstimator(target)
			};

			if (world.Map.Contains(from))
				search.AddInitialCell(from);

			return search;
		}

		public static IPathSearch FromPoints(IWorld world, IMobileInfo mi, IActor self, IEnumerable<CPos> froms, CPos target, bool checkForBlocked)
		{
			var graph = new PathGraph(CellInfoLayerManager.Instance.NewLayer(world.Map), mi, self, world, checkForBlocked);
			var search = new PathSearch(graph)
			{
				heuristic = DefaultEstimator(target)
			};

			foreach (var sl in froms.Where(sl => world.Map.Contains(sl)))
				search.AddInitialCell(sl);

			return search;
		}

		protected override void AddInitialCell(CPos location)
		{
			Graph[location] = new CellInfo(0, heuristic(location), location, CellStatus.Open);
			OpenQueue.Add(location);
			startPoints.Add(location);
			considered.AddLast(new Pair<CPos, int>(location, 0));
		}

		#endregion

		/// <summary>
		/// This function analyzes the neighbors of the most promising node in the Pathfinding graph
		/// using the A* algorithm (A-star) and returns that node
		/// </summary>
		/// <returns>The most promising node of the iteration</returns>
		public override CPos Expand()
		{
			var currentMinNode = OpenQueue.Pop();

			var currentCell = Graph[currentMinNode];
			Graph[currentMinNode] = new CellInfo(currentCell.CostSoFar, currentCell.EstimatedTotal, currentCell.PreviousPos, CellStatus.Closed);

			if (Graph.CustomCost != null)
			{
				var c = Graph.CustomCost(currentMinNode);
				if (c == int.MaxValue)
					return currentMinNode;
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

				Graph[neighborCPos] = new CellInfo(gCost, gCost + hCost, currentMinNode, CellStatus.Open);

				if (neighborCell.Status != CellStatus.Open)
					OpenQueue.Add(neighborCPos);

				if (Debug)
				{
					if (gCost > MaxCost)
						MaxCost = gCost;

					considered.AddLast(new Pair<CPos, int>(neighborCPos, gCost));
				}
			}

			return currentMinNode;
		}
	}
}
