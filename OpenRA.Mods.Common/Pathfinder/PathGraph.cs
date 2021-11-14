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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// Represents a graph with nodes and edges
	/// </summary>
	/// <typeparam name="T">The type of node used in the graph</typeparam>
	public interface IGraph<T> : IDisposable
	{
		/// <summary>
		/// Gets all the Connections for a given node in the graph
		/// </summary>
		List<GraphConnection> GetConnections(CPos position);

		/// <summary>
		/// Retrieves an object given a node in the graph
		/// </summary>
		T this[CPos pos] { get; set; }

		Func<CPos, bool> CustomBlock { get; set; }

		Func<CPos, int> CustomCost { get; set; }

		int LaneBias { get; set; }

		bool InReverse { get; set; }

		Actor IgnoreActor { get; set; }

		World World { get; }

		Actor Actor { get; }
	}

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
			Destination = destination;
			Cost = cost;
		}
	}

	sealed class PathGraph : IGraph<CellInfo>
	{
		public const int PathCostForInvalidPath = int.MaxValue;
		public const short MovementCostForUnreachableCell = short.MaxValue;

		public Actor Actor { get; private set; }
		public World World { get; private set; }
		public Func<CPos, bool> CustomBlock { get; set; }
		public Func<CPos, int> CustomCost { get; set; }
		public int LaneBias { get; set; }
		public bool InReverse { get; set; }
		public Actor IgnoreActor { get; set; }

		readonly BlockedByActor checkConditions;
		readonly Locomotor locomotor;
		readonly CellInfoLayerPool.PooledCellInfoLayer pooledLayer;
		readonly bool checkTerrainHeight;
		readonly CellLayer<CellInfo>[] cellInfoForLayer;

		public PathGraph(CellInfoLayerPool layerPool, Locomotor locomotor, Actor actor, World world, BlockedByActor check)
		{
			this.locomotor = locomotor;

			// As we support a search over the whole map area,
			// use the pool to grab the CellInfos we need to track the graph state.
			// This allows us to avoid the cost of allocating large arrays constantly.
			// PERF: Avoid LINQ
			var cmls = world.GetCustomMovementLayers();
			pooledLayer = layerPool.Get();
			cellInfoForLayer = new CellLayer<CellInfo>[cmls.Length];
			cellInfoForLayer[0] = pooledLayer.GetLayer();
			foreach (var cml in cmls)
				if (cml != null && cml.EnabledForLocomotor(locomotor.Info))
					cellInfoForLayer[cml.Index] = pooledLayer.GetLayer();

			World = world;
			Actor = actor;
			LaneBias = 1;
			checkConditions = check;
			checkTerrainHeight = world.Map.Grid.MaximumTerrainHeight > 0;
		}

		// Sets of neighbors for each incoming direction. These exclude the neighbors which are guaranteed
		// to be reached more cheaply by a path through our parent cell which does not include the current cell.
		// For horizontal/vertical directions, the set is the three cells 'ahead'. For diagonal directions, the set
		// is the three cells ahead, plus the two cells to the side. Effectively, these are the cells left over
		// if you ignore the ones reachable from the parent cell.
		// We can do this because for any cell in range of both the current and parent location,
		// if we can reach it from one we are guaranteed to be able to reach it from the other.
		static readonly CVec[][] DirectedNeighbors =
		{
			new[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1), new CVec(-1, 0), new CVec(-1, 1) }, // TL
			new[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1) }, // T
			new[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1), new CVec(1, 0), new CVec(1, 1) }, // TR
			new[] { new CVec(-1, -1), new CVec(-1, 0), new CVec(-1, 1) }, // L
			CVec.Directions,
			new[] { new CVec(1, -1), new CVec(1, 0), new CVec(1, 1) }, // R
			new[] { new CVec(-1, -1), new CVec(-1, 0), new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) }, // BL
			new[] { new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) }, // B
			new[] { new CVec(1, -1), new CVec(1, 0), new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) }, // BR
		};

		// With height discontinuities between the parent and current cell, we cannot optimize the possible neighbors.
		// It is no longer true that for any cell in range of both the current and parent location,
		// if we can reach it from one we are guaranteed to be able to reach it from the other.
		// This is because a height discontinuity may have prevented the parent location from reaching,
		// but our current cell on a new height may be able to reach as the height difference may be small enough.
		// Therefore, we can only exclude the parent cell in each set of directions.
		static readonly CVec[][] DirectedNeighborsConservative =
		{
			CVec.Directions.Exclude(new CVec(1, 1)).ToArray(), // TL
			CVec.Directions.Exclude(new CVec(0, 1)).ToArray(), // T
			CVec.Directions.Exclude(new CVec(-1, 1)).ToArray(), // TR
			CVec.Directions.Exclude(new CVec(1, 0)).ToArray(), // L
			CVec.Directions,
			CVec.Directions.Exclude(new CVec(-1, 0)).ToArray(), // R
			CVec.Directions.Exclude(new CVec(1, -1)).ToArray(), // BL
			CVec.Directions.Exclude(new CVec(0, -1)).ToArray(), // B
			CVec.Directions.Exclude(new CVec(-1, -1)).ToArray(), // BR
		};

		public List<GraphConnection> GetConnections(CPos position)
		{
			var layer = position.Layer;
			var info = cellInfoForLayer[layer];
			var previousNode = info[position].PreviousNode;

			var dx = position.X - previousNode.X;
			var dy = position.Y - previousNode.Y;
			var index = dy * 3 + dx + 4;

			var heightLayer = World.Map.Height;
			var directions =
				(checkTerrainHeight && layer == 0 && previousNode.Layer == 0 && heightLayer[position] != heightLayer[previousNode]
				? DirectedNeighborsConservative
				: DirectedNeighbors)[index];

			var validNeighbors = new List<GraphConnection>(directions.Length + (layer == 0 ? cellInfoForLayer.Length : 1));
			for (var i = 0; i < directions.Length; i++)
			{
				var dir = directions[i];
				var neighbor = position + dir;
				var pathCost = GetPathCostToNode(position, neighbor, dir);

				// PERF: Skip closed cells already, 15% of all cells
				if (pathCost != PathCostForInvalidPath && info[neighbor].Status != CellStatus.Closed)
					validNeighbors.Add(new GraphConnection(neighbor, pathCost));
			}

			var cmls = World.GetCustomMovementLayers();
			if (layer == 0)
			{
				foreach (var cml in cmls)
				{
					if (cml == null || !cml.EnabledForLocomotor(locomotor.Info))
						continue;

					var layerPosition = new CPos(position.X, position.Y, cml.Index);
					var entryCost = cml.EntryMovementCost(locomotor.Info, layerPosition);
					if (entryCost != MovementCostForUnreachableCell &&
						CanEnterNode(position, layerPosition) &&
						this[layerPosition].Status != CellStatus.Closed)
						validNeighbors.Add(new GraphConnection(layerPosition, entryCost));
				}
			}
			else
			{
				var layerPosition = new CPos(position.X, position.Y, 0);
				var exitCost = cmls[layer].ExitMovementCost(locomotor.Info, layerPosition);
				if (exitCost != MovementCostForUnreachableCell &&
					CanEnterNode(position, layerPosition) &&
					this[layerPosition].Status != CellStatus.Closed)
					validNeighbors.Add(new GraphConnection(layerPosition, exitCost));
			}

			return validNeighbors;
		}

		bool CanEnterNode(CPos srcNode, CPos destNode)
		{
			return
				locomotor.MovementCostToEnterCell(Actor, srcNode, destNode, checkConditions, IgnoreActor)
				!= MovementCostForUnreachableCell;
		}

		int GetPathCostToNode(CPos srcNode, CPos destNode, CVec direction)
		{
			var movementCost = locomotor.MovementCostToEnterCell(Actor, srcNode, destNode, checkConditions, IgnoreActor);
			if (movementCost != MovementCostForUnreachableCell && !(CustomBlock != null && CustomBlock(destNode)))
				return CalculateCellPathCost(destNode, direction, movementCost);

			return PathCostForInvalidPath;
		}

		int CalculateCellPathCost(CPos neighborCPos, CVec direction, short movementCost)
		{
			var cellCost = direction.X * direction.Y != 0
				? Exts.MultiplyBySqrtTwo(movementCost)
				: movementCost;

			if (CustomCost != null)
			{
				var customCost = CustomCost(neighborCPos);
				if (customCost == PathCostForInvalidPath)
					return PathCostForInvalidPath;

				cellCost += customCost;
			}

			// Directional bonuses for smoother flow!
			if (LaneBias != 0)
			{
				var ux = neighborCPos.X + (InReverse ? 1 : 0) & 1;
				var uy = neighborCPos.Y + (InReverse ? 1 : 0) & 1;

				if ((ux == 0 && direction.Y < 0) || (ux == 1 && direction.Y > 0))
					cellCost += LaneBias;

				if ((uy == 0 && direction.X < 0) || (uy == 1 && direction.X > 0))
					cellCost += LaneBias;
			}

			return cellCost;
		}

		public CellInfo this[CPos pos]
		{
			get => cellInfoForLayer[pos.Layer][pos];
			set => cellInfoForLayer[pos.Layer][pos] = value;
		}

		public void Dispose()
		{
			pooledLayer.Dispose();
		}
	}
}
