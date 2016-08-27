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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Pathfinder
{
	public interface IPathSearch : IDisposable
	{
		/// <summary>
		/// The Graph used by the A*
		/// </summary>
		IGraph<CellInfo> Graph { get; }

		/// <summary>
		/// Stores the analyzed nodes by the expand function
		/// </summary>
		IEnumerable<Pair<CPos, int>> Considered { get; }

		Player Owner { get; }

		int MaxCost { get; }

		IPathSearch Reverse();

		IPathSearch WithCustomBlocker(Func<CPos, bool> customBlock);

		IPathSearch WithIgnoredActor(Actor b);

		IPathSearch WithHeuristic(Func<CPos, int> h);

		IPathSearch WithCustomCost(Func<CPos, int> w);

		IPathSearch WithoutLaneBias();

		IPathSearch FromPoint(CPos from);

		/// <summary>
		/// Decides whether a location is a target based on its estimate
		/// (An estimate of 0 means that the location and the unit's goal
		/// are the same. There could be multiple goals).
		/// </summary>
		/// <param name="location">The location to assess</param>
		/// <returns>Whether the location is a target</returns>
		bool IsTarget(CPos location);

		bool CanExpand { get; }
		CPos Expand();
	}

	public abstract class BasePathSearch : IPathSearch
	{
		public IGraph<CellInfo> Graph { get; set; }

		protected IPriorityQueue<GraphConnection> OpenQueue { get; private set; }

		public abstract IEnumerable<Pair<CPos, int>> Considered { get; }

		public Player Owner { get { return Graph.Actor.Owner; } }
		public int MaxCost { get; protected set; }
		public bool Debug { get; set; }
		protected Func<CPos, int> heuristic;
		protected Func<CPos, bool> isGoal;

		// This member is used to compute the ID of PathSearch.
		// Essentially, it represents a collection of the initial
		// points considered and their Heuristics to reach
		// the target. It pretty match identifies, in conjunction of the Actor,
		// a deterministic set of calculations
		protected readonly IPriorityQueue<GraphConnection> StartPoints;

		protected BasePathSearch(IGraph<CellInfo> graph)
		{
			Graph = graph;
			OpenQueue = new PriorityQueue<GraphConnection>(GraphConnection.ConnectionCostComparer);
			StartPoints = new PriorityQueue<GraphConnection>(GraphConnection.ConnectionCostComparer);
			MaxCost = 0;
		}

		/// <summary>
		/// Default: Diagonal distance heuristic. More information:
		/// http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html
		/// </summary>
		/// <returns>A delegate that calculates the estimation for a node</returns>
		protected static Func<CPos, int> DefaultEstimator(CPos destination)
		{
			return here =>
			{
				var diag = Math.Min(Math.Abs(here.X - destination.X), Math.Abs(here.Y - destination.Y));
				var straight = Math.Abs(here.X - destination.X) + Math.Abs(here.Y - destination.Y);

				// According to the information link, this is the shape of the function.
				// We just extract factors to simplify.
				// Possible simplification: var h = Constants.CellCost * (straight + (Constants.Sqrt2 - 2) * diag);
				return Constants.CellCost * straight + (Constants.DiagonalCellCost - 2 * Constants.CellCost) * diag;
			};
		}

		public IPathSearch Reverse()
		{
			Graph.InReverse = true;
			return this;
		}

		public IPathSearch WithCustomBlocker(Func<CPos, bool> customBlock)
		{
			Graph.CustomBlock = customBlock;
			return this;
		}

		public IPathSearch WithIgnoredActor(Actor b)
		{
			Graph.IgnoredActor = b;
			return this;
		}

		public IPathSearch WithHeuristic(Func<CPos, int> h)
		{
			heuristic = h;
			return this;
		}

		public IPathSearch WithCustomCost(Func<CPos, int> w)
		{
			Graph.CustomCost = w;
			return this;
		}

		public IPathSearch WithoutLaneBias()
		{
			Graph.LaneBias = 0;
			return this;
		}

		public IPathSearch FromPoint(CPos from)
		{
			if (Graph.World.Map.Contains(from))
				AddInitialCell(from);

			return this;
		}

		protected abstract void AddInitialCell(CPos cell);

		public bool IsTarget(CPos location)
		{
			return isGoal(location);
		}

		public bool CanExpand { get { return !OpenQueue.Empty; } }
		public abstract CPos Expand();

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
