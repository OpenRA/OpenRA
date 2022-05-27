#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	/// <summary>
	/// Represents a pathfinding graph with nodes and edges.
	/// Nodes are represented as cells, and pathfinding information
	/// in the form of <see cref="CellInfo"/> is attached to each one.
	/// </summary>
	public interface IPathGraph : IDisposable
	{
		/// <summary>
		/// Given a source node, returns connections to all reachable destination nodes with their cost.
		/// </summary>
		List<GraphConnection> GetConnections(CPos source);

		/// <summary>
		/// Gets or sets the pathfinding information for a given node.
		/// </summary>
		CellInfo this[CPos node] { get; set; }
	}

	public static class PathGraph
	{
		public const int PathCostForInvalidPath = int.MaxValue;
		public const short MovementCostForUnreachableCell = short.MaxValue;
	}

	/// <summary>
	/// Represents part of an edge in a graph, giving the cost to traverse to a node.
	/// </summary>
	public readonly struct GraphConnection
	{
		public static readonly CostComparer ConnectionCostComparer = CostComparer.Instance;

		public sealed class CostComparer : IComparer<GraphConnection>
		{
			public static readonly CostComparer Instance = new CostComparer();
			CostComparer() { }
			public int Compare(GraphConnection x, GraphConnection y)
			{
				return x.Cost.CompareTo(y.Cost);
			}
		}

		public readonly CPos Destination;
		public readonly int Cost;

		public GraphConnection(CPos destination, int cost)
		{
			if (cost < 0)
				throw new ArgumentOutOfRangeException(nameof(cost), $"{nameof(cost)} cannot be negative");
			if (cost == PathGraph.PathCostForInvalidPath)
				throw new ArgumentOutOfRangeException(nameof(cost), $"{nameof(cost)} cannot be used for an unreachable path");

			Destination = destination;
			Cost = cost;
		}

		public override string ToString() => $"-> {Destination} = {Cost}";
	}
}
