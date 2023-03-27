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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// A dense pathfinding graph that implements the ability to cost and get connections for cells,
	/// and supports <see cref="ICustomMovementLayer"/>. Allows searching over a dense grid of cells.
	/// Derived classes are required to provide backing storage for the pathfinding information.
	/// </summary>
	abstract class DensePathGraph : IPathGraph
	{
		const int LaneBiasCost = 1;

		protected readonly ICustomMovementLayer[] CustomMovementLayers;
		readonly int customMovementLayersEnabledForLocomotor;
		readonly Locomotor locomotor;
		readonly Actor actor;
		readonly World world;
		readonly BlockedByActor check;
		readonly Func<CPos, int> customCost;
		readonly Actor ignoreActor;
		readonly bool laneBias;
		readonly bool inReverse;
		readonly bool checkTerrainHeight;

		protected DensePathGraph(Locomotor locomotor, Actor actor, World world, BlockedByActor check,
			Func<CPos, int> customCost, Actor ignoreActor, bool laneBias, bool inReverse)
		{
			CustomMovementLayers = world.GetCustomMovementLayers();
			customMovementLayersEnabledForLocomotor = CustomMovementLayers.Count(cml => cml != null && cml.EnabledForLocomotor(locomotor.Info));
			this.locomotor = locomotor;
			this.world = world;
			this.actor = actor;
			this.check = check;
			this.customCost = customCost;
			this.ignoreActor = ignoreActor;
			this.laneBias = laneBias;
			this.inReverse = inReverse;
			checkTerrainHeight = world.Map.Grid.MaximumTerrainHeight > 0;
		}

		public abstract CellInfo this[CPos node] { get; set; }

		/// <summary>
		/// Determines if a candidate neighbouring position is
		/// allowable to be returned in a <see cref="GraphConnection"/>.
		/// </summary>
		/// <param name="neighbor">The candidate cell. This might not lie within map bounds.</param>
		protected virtual bool IsValidNeighbor(CPos neighbor)
		{
			return true;
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

		public List<GraphConnection> GetConnections(CPos position, Func<CPos, bool> targetPredicate)
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
				if (!IsValidNeighbor(neighbor))
					continue;

				var pathCost = GetPathCostToNode(position, neighbor, dir, targetPredicate);
				if (pathCost != PathGraph.PathCostForInvalidPath &&
					this[neighbor].Status != CellStatus.Closed)
					validNeighbors.Add(new GraphConnection(neighbor, pathCost));
			}

			if (layer == 0)
			{
				foreach (var cml in CustomMovementLayers)
				{
					if (cml == null || !cml.EnabledForLocomotor(locomotor.Info))
						continue;

					var layerPosition = new CPos(position.X, position.Y, cml.Index);
					if (!IsValidNeighbor(layerPosition))
						continue;

					var entryCost = cml.EntryMovementCost(locomotor.Info, layerPosition);
					if (entryCost != PathGraph.MovementCostForUnreachableCell &&
						CanEnterNode(position, layerPosition, targetPredicate) &&
						this[layerPosition].Status != CellStatus.Closed)
						validNeighbors.Add(new GraphConnection(layerPosition, entryCost));
				}
			}
			else
			{
				var groundPosition = new CPos(position.X, position.Y, 0);
				if (IsValidNeighbor(groundPosition))
				{
					var exitCost = CustomMovementLayers[layer].ExitMovementCost(locomotor.Info, groundPosition);
					if (exitCost != PathGraph.MovementCostForUnreachableCell &&
						CanEnterNode(position, groundPosition, targetPredicate) &&
						this[groundPosition].Status != CellStatus.Closed)
						validNeighbors.Add(new GraphConnection(groundPosition, exitCost));
				}
			}

			return validNeighbors;
		}

		bool CanEnterNode(CPos srcNode, CPos destNode, Func<CPos, bool> targetPredicate)
		{
			return
				locomotor.MovementCostToEnterCell(actor, srcNode, destNode, check, ignoreActor)
				!= PathGraph.MovementCostForUnreachableCell ||
				(inReverse && targetPredicate(destNode));
		}

		int GetPathCostToNode(CPos srcNode, CPos destNode, CVec direction, Func<CPos, bool> targetPredicate)
		{
			var movementCost = locomotor.MovementCostToEnterCell(actor, srcNode, destNode, check, ignoreActor);

			// When doing searches in reverse, we must allow movement onto an inaccessible target location.
			// Because when reversed this is actually the source, and it is allowed to move out from an inaccessible source.
			if (movementCost == PathGraph.MovementCostForUnreachableCell && inReverse && targetPredicate(destNode))
				movementCost = 0;

			if (movementCost != PathGraph.MovementCostForUnreachableCell)
				return CalculateCellPathCost(destNode, direction, movementCost);

			return PathGraph.PathCostForInvalidPath;
		}

		int CalculateCellPathCost(CPos neighborCPos, CVec direction, short movementCost)
		{
			var cellCost = direction.X * direction.Y != 0
				? Exts.MultiplyBySqrtTwo(movementCost)
				: movementCost;

			if (customCost != null)
			{
				var customCellCost = customCost(neighborCPos);
				if (customCellCost == PathGraph.PathCostForInvalidPath)
					return PathGraph.PathCostForInvalidPath;

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

		protected virtual void Dispose(bool disposing) { }

		public void Dispose()
		{
			Dispose(true);
		}
	}
}
