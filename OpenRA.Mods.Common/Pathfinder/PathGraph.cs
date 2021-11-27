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
	/// A dense pathfinding graph that supports a search over all cells within a map.
	/// It implements the ability to cost and get connections for cells, and supports <see cref="ICustomMovementLayer"/>.
	/// </summary>
	sealed class PathGraph : IPathGraph
	{
		public const int PathCostForInvalidPath = int.MaxValue;
		public const short MovementCostForUnreachableCell = short.MaxValue;
		const int LaneBiasCost = 1;

		readonly ICustomMovementLayer[] customMovementLayers;
		readonly int customMovementLayersEnabledForLocomotor;
		readonly Locomotor locomotor;
		readonly Actor actor;
		readonly World world;
		readonly BlockedByActor check;
		readonly Func<CPos, int> customCost;
		readonly Actor ignoreActor;
		readonly bool inReverse;
		readonly bool laneBias;
		readonly bool checkTerrainHeight;
		readonly CellInfoLayerPool.PooledCellInfoLayer pooledLayer;
		readonly CellLayer<CellInfo>[] cellInfoForLayer;

		public PathGraph(CellInfoLayerPool layerPool, Locomotor locomotor, Actor actor, World world, BlockedByActor check,
			Func<CPos, int> customCost, Actor ignoreActor, bool inReverse, bool laneBias)
		{
			customMovementLayers = world.GetCustomMovementLayers();
			customMovementLayersEnabledForLocomotor = customMovementLayers.Count(cml => cml != null && cml.EnabledForLocomotor(locomotor.Info));
			this.locomotor = locomotor;
			this.world = world;
			this.actor = actor;
			this.check = check;
			this.customCost = customCost;
			this.ignoreActor = ignoreActor;
			this.inReverse = inReverse;
			this.laneBias = laneBias;
			checkTerrainHeight = world.Map.Grid.MaximumTerrainHeight > 0;

			// As we support a search over the whole map area,
			// use the pool to grab the CellInfos we need to track the graph state.
			// This allows us to avoid the cost of allocating large arrays constantly.
			// PERF: Avoid LINQ
			pooledLayer = layerPool.Get();
			cellInfoForLayer = new CellLayer<CellInfo>[customMovementLayers.Length];
			cellInfoForLayer[0] = pooledLayer.GetLayer();
			foreach (var cml in customMovementLayers)
				if (cml != null && cml.EnabledForLocomotor(locomotor.Info))
					cellInfoForLayer[cml.Index] = pooledLayer.GetLayer();
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
			var info = this[position];
			var previousNode = info.PreviousNode;

			var dx = position.X - previousNode.X;
			var dy = position.Y - previousNode.Y;
			var index = dy * 3 + dx + 4;

			var heightLayer = world.Map.Height;
			var directions =
				(checkTerrainHeight && layer == 0 && previousNode.Layer == 0 && heightLayer[position] != heightLayer[previousNode]
				? DirectedNeighborsConservative
				: DirectedNeighbors)[index];

			var validNeighbors = new List<GraphConnection>(directions.Length + (layer == 0 ? customMovementLayersEnabledForLocomotor : 1));
			for (var i = 0; i < directions.Length; i++)
			{
				var dir = directions[i];
				var neighbor = position + dir;

				var pathCost = GetPathCostToNode(position, neighbor, dir);
				if (pathCost != PathCostForInvalidPath &&
					this[neighbor].Status != CellStatus.Closed)
					validNeighbors.Add(new GraphConnection(neighbor, pathCost));
			}

			if (layer == 0)
			{
				foreach (var cml in customMovementLayers)
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
				var groundPosition = new CPos(position.X, position.Y, 0);
				var exitCost = customMovementLayers[layer].ExitMovementCost(locomotor.Info, groundPosition);
				if (exitCost != MovementCostForUnreachableCell &&
					CanEnterNode(position, groundPosition) &&
					this[groundPosition].Status != CellStatus.Closed)
					validNeighbors.Add(new GraphConnection(groundPosition, exitCost));
			}

			return validNeighbors;
		}

		bool CanEnterNode(CPos srcNode, CPos destNode)
		{
			return
				locomotor.MovementCostToEnterCell(actor, srcNode, destNode, check, ignoreActor)
				!= MovementCostForUnreachableCell;
		}

		int GetPathCostToNode(CPos srcNode, CPos destNode, CVec direction)
		{
			var movementCost = locomotor.MovementCostToEnterCell(actor, srcNode, destNode, check, ignoreActor);
			if (movementCost != MovementCostForUnreachableCell)
				return CalculateCellPathCost(destNode, direction, movementCost);

			return PathCostForInvalidPath;
		}

		int CalculateCellPathCost(CPos neighborCPos, CVec direction, short movementCost)
		{
			var cellCost = direction.X * direction.Y != 0
				? Exts.MultiplyBySqrtTwo(movementCost)
				: movementCost;

			if (customCost != null)
			{
				var customCellCost = customCost(neighborCPos);
				if (customCellCost == PathCostForInvalidPath)
					return PathCostForInvalidPath;

				cellCost += customCellCost;
			}

			// Directional bonuses for smoother flow!
			if (laneBias)
			{
				var ux = neighborCPos.X + (inReverse ? 1 : 0) & 1;
				var uy = neighborCPos.Y + (inReverse ? 1 : 0) & 1;

				if ((ux == 0 && direction.Y < 0) || (ux == 1 && direction.Y > 0))
					cellCost += LaneBiasCost;

				if ((uy == 0 && direction.X < 0) || (uy == 1 && direction.X > 0))
					cellCost += LaneBiasCost;
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
