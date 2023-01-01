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
		/// <remarks>PERF: Returns a <see cref="List{T}"/> rather than an <see cref="IEnumerable{T}"/> as enumerating
		/// this efficiently is important for pathfinding performance. Callers should interact with this as an
		/// <see cref="IEnumerable{T}"/> and not mutate the result.</remarks>
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
	/// Represents a full edge in a graph, giving the cost to traverse between two nodes.
	/// </summary>
	public readonly struct GraphEdge
	{
		public readonly CPos Source;
		public readonly CPos Destination;
		public readonly int Cost;

		public GraphEdge(CPos source, CPos destination, int cost)
		{
			if (source == destination)
				throw new ArgumentException($"{nameof(source)} and {nameof(destination)} must refer to different cells");
			if (cost < 0)
				throw new ArgumentOutOfRangeException(nameof(cost), $"{nameof(cost)} cannot be negative");
			if (cost == PathGraph.PathCostForInvalidPath)
				throw new ArgumentOutOfRangeException(nameof(cost), $"{nameof(cost)} cannot be used for an unreachable path");

			Source = source;
			Destination = destination;
			Cost = cost;
		}

		public GraphConnection ToConnection()
		{
			return new GraphConnection(Destination, Cost);
		}

		public override string ToString() => $"{Source} -> {Destination} = {Cost}";
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

		public GraphEdge ToEdge(CPos source)
		{
			return new GraphEdge(source, Destination, Cost);
		}

		public override string ToString() => $"-> {Destination} = {Cost}";
	}
}
