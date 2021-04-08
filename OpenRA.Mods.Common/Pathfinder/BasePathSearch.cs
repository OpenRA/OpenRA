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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	public enum PathQueryType
	{
		// Destination unknown. Search until `IsGoal` returns true.
		ConditionUnidirectional,

		// Destination known. Search from 'from' to 'to'.
		PositionUnidirectional,

		// Destination known. Search from both 'from' and 'to', meet in middle.
		PositionBidirectional
	}

	public class PathQuery
	{
		public readonly PathQueryType QueryType;
		public readonly World World;
		public readonly Locomotor Locomotor;
		public readonly Actor Actor;
		public readonly BlockedByActor Check;
		public readonly IEnumerable<CPos> FromPositions;

		// To be set for Position searches.
		public readonly CPos? ToPosition;

		public readonly Func<CPos, bool> CustomBlock;
		public readonly Actor IgnoreActor;
		public readonly Func<CPos, int> CustomCost;
		public readonly Func<CPos, bool> IsGoal;
		public readonly bool LaneBiasDisabled;

		// The other end of a bidirectional search.
		public readonly bool Reverse;

		public PathQuery(PathQueryType queryType,
			World world,
			Locomotor locomotor,
			Actor actor,
			BlockedByActor check,
			IEnumerable<CPos> fromPositions,
			CPos? toPosition = null,
			Func<CPos, bool> customBlock = null,
			Actor ignoreActor = null,
			Func<CPos, int> customCost = null,
			Func<CPos, bool> isGoal = null,
			bool laneBiasDisabled = false,
			bool reverse = false)
		{
			QueryType = queryType;
			World = world;
			Locomotor = locomotor;
			Actor = actor;
			Check = check;
			FromPositions = fromPositions;
			ToPosition = toPosition;
			CustomBlock = customBlock;
			IgnoreActor = ignoreActor;
			CustomCost = customCost;
			IsGoal = isGoal;
			LaneBiasDisabled = laneBiasDisabled;
			Reverse = reverse;
		}

		public PathQuery(PathQueryType queryType,
			World world,
			Locomotor locomotor,
			Actor actor,
			BlockedByActor check,
			CPos fromPosition,
			CPos? toPosition = null,
			Func<CPos, bool> customBlock = null,
			Actor ignoreActor = null,
			Func<CPos, int> customCost = null,
			Func<CPos, bool> isGoal = null,
			bool laneBiasDisabled = false,
			bool reverse = false)
		{
			QueryType = queryType;
			World = world;
			Locomotor = locomotor;
			Actor = actor;
			Check = check;
			FromPositions = new[] { fromPosition };
			ToPosition = toPosition;
			CustomBlock = customBlock;
			IgnoreActor = ignoreActor;
			CustomCost = customCost;
			IsGoal = isGoal;
			LaneBiasDisabled = laneBiasDisabled;
			Reverse = reverse;
		}

		public PathQuery CreateReverse()
		{
			if (QueryType != PathQueryType.PositionBidirectional)
				throw new ArgumentException("Only bidirectional queries use a reverse");

			if (!ToPosition.HasValue)
				throw new ArgumentException("ToPosition not set");

			if (FromPositions.Count() > 1)
				throw new ArgumentException("Reverse requires a single FromPosition");

			return new PathQuery(
				QueryType,
				World,
				Locomotor,
				Actor,
				Check,
				new[] { ToPosition.Value },
				FromPositions.First(),
				CustomBlock,
				IgnoreActor,
				CustomCost,
				IsGoal,
				LaneBiasDisabled,
				!Reverse);
		}
	}

	public abstract class BasePathSearch : IDisposable
	{
		public readonly PathGraph Graph;
		public readonly PathQuery Query;
		public readonly Func<CPos, bool> IsGoal;
		public readonly bool Debug;

		// Stores maximum cost in debug mode. Zero otherwise.
		public int MaxCost { get; protected set; }

		// Stores considered cells in debug mode. Empty otherwise.
		public abstract IEnumerable<(CPos Cell, int Cost)> Considered { get; }

		protected IPriorityQueue<GraphConnection> OpenQueue { get; private set; }

		protected readonly Func<CPos, int> heuristic;
		protected readonly int heuristicWeightPercentage;
		readonly int cellCost, diagonalCellCost;

		protected BasePathSearch(PathGraph graph, PathQuery query, bool debug)
		{
			Debug = debug;
			Graph = graph;
			Query = query;
			OpenQueue = new PriorityQueue<GraphConnection>(GraphConnection.ConnectionCostComparer);
			MaxCost = 0;

			// Determine the minimum possible cost for moving horizontally between cells based on terrain speeds.
			// The minimum possible cost diagonally is then Sqrt(2) times more costly.
			cellCost = ((Mobile)Query.Actor.OccupiesSpace).Info.LocomotorInfo.TerrainSpeeds.Values.Min(ti => ti.Cost);
			diagonalCellCost = cellCost * 141421 / 100000;

			if (Query.QueryType == PathQueryType.ConditionUnidirectional)
			{
				if (Query.IsGoal == null)
					throw new ArgumentException("IsGoal not set in path query");

				IsGoal = Query.IsGoal;
				heuristicWeightPercentage = 100;
				heuristic = loc => 0;
			}
			else
			{
				if (Query.FromPositions == null)
					throw new ArgumentException("FromPositions not set in path query");

				if (!Query.ToPosition.HasValue)
					throw new ArgumentException("ToPosition set in path query");

				IsGoal = loc =>
				{
					var locInfo = graph[loc];
					return locInfo.EstimatedTotal - locInfo.CostSoFar == 0;
				};

				// The search will aim for the shortest path by default, a weight of 100%.
				// We can allow the search to find paths that aren't optimal by changing the weight.
				// We provide a weight that limits the worst case length of the path,
				// e.g. a weight of 110% will find a path no more than 10% longer than the shortest possible.
				// The benefit of allowing the search to return suboptimal paths is faster computation time.
				// The search can skip some areas of the search space, meaning it has less work to do.
				// We allow paths up to 25% longer than the shortest, optimal path, to improve pathfinding time.
				heuristicWeightPercentage = 125;
				heuristic = DefaultEstimator(query.ToPosition.Value);
			}
		}

		/// <summary>
		/// Default: Diagonal distance heuristic. More information:
		/// http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html
		/// </summary>
		/// <returns>A delegate that calculates the estimation for a node</returns>
		Func<CPos, int> DefaultEstimator(CPos destination)
		{
			return here =>
			{
				var diag = Math.Min(Math.Abs(here.X - destination.X), Math.Abs(here.Y - destination.Y));
				var straight = Math.Abs(here.X - destination.X) + Math.Abs(here.Y - destination.Y);

				// According to the information link, this is the shape of the function.
				// We just extract factors to simplify.
				// Possible simplification: var h = Constants.CellCost * (straight + (Constants.Sqrt2 - 2) * diag);
				return (cellCost * straight + (diagonalCellCost - 2 * cellCost) * diag) * heuristicWeightPercentage / 100;
			};
		}

		public bool CanExpand => !OpenQueue.Empty;
		public abstract bool TryExpand(out CPos mostPromisingNode);

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				Graph.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
