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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
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

	public class PathGraph
	{
		public const int CostForInvalidCell = int.MaxValue;

		public readonly PathQuery Query;

		readonly CellInfoLayerPool.PooledCellInfoLayer pooledLayer;
		readonly int laneBias;
		readonly bool checkTerrainHeight;
		CellLayer<CellInfo> groundInfo;

		readonly Dictionary<byte, (ICustomMovementLayer Layer, CellLayer<CellInfo> Info)> customLayerInfo =
			new Dictionary<byte, (ICustomMovementLayer, CellLayer<CellInfo>)>();

		public PathGraph(CellInfoLayerPool layerPool, PathQuery query)
		{
			Query = query;
			pooledLayer = layerPool.Get();
			groundInfo = pooledLayer.GetLayer();

			var locomotorInfo = Query.Locomotor.Info;
			var actorInfo = Query.Actor.Info;

			// PERF: Avoid LINQ
			foreach (var cml in Query.World.GetCustomMovementLayers().Values)
				if (cml.EnabledForActor(actorInfo, locomotorInfo))
					customLayerInfo[cml.Index] = (cml, pooledLayer.GetLayer());

			laneBias = Query.LaneBiasDisabled ? 0 : 1;
			checkTerrainHeight = Query.World.Map.Grid.MaximumTerrainHeight > 0;
		}

		// Sets of neighbors for each incoming direction. These exclude the neighbors which are guaranteed
		// to be reached more cheaply by a path through our parent cell which does not include the current cell.
		// For horizontal/vertical directions, the set is the three cells 'ahead'. For diagonal directions, the set
		// is the three cells ahead, plus the two cells to the side, which we cannot exclude without knowing if
		// the cell directly between them and our parent is passable.
		static readonly CVec[][] DirectedNeighbors =
		{
			new[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1), new CVec(-1, 0), new CVec(-1, 1) },
			new[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1) },
			new[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1), new CVec(1, 0), new CVec(1, 1) },
			new[] { new CVec(-1, -1), new CVec(-1, 0), new CVec(-1, 1) },
			CVec.Directions,
			new[] { new CVec(1, -1), new CVec(1, 0), new CVec(1, 1) },
			new[] { new CVec(-1, -1), new CVec(-1, 0), new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) },
			new[] { new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) },
			new[] { new CVec(1, -1), new CVec(1, 0), new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) },
		};

		public List<GraphConnection> GetConnections(CPos position)
		{
			var posLayer = position.Layer;
			var info = posLayer == 0 ? groundInfo : customLayerInfo[posLayer].Info;
			var previousPos = info[position].PreviousPos;

			var dx = position.X - previousPos.X;
			var dy = position.Y - previousPos.Y;
			var index = dy * 3 + dx + 4;

			var directions = DirectedNeighbors[index];
			var validNeighbors = new List<GraphConnection>(directions.Length + (posLayer == 0 ? customLayerInfo.Count : 1));
			for (var i = 0; i < directions.Length; i++)
			{
				var dir = directions[i];
				var neighbor = position + dir;
				var movementCost = GetCostToNode(neighbor, dir);

				// PERF: Skip closed cells already, 15% of all cells
				if (movementCost != CostForInvalidCell && info[neighbor].Status != CellStatus.Closed)
					validNeighbors.Add(new GraphConnection(neighbor, movementCost));
			}

			var actorInfo = Query.Actor.Info;
			var locomotorInfo = Query.Locomotor.Info;
			if (position.Layer == 0)
			{
				foreach (var cli in customLayerInfo.Values)
				{
					var layerPosition = new CPos(position.X, position.Y, cli.Layer.Index);
					var entryCost = cli.Layer.EntryMovementCost(actorInfo, locomotorInfo, layerPosition);
					if (entryCost != CostForInvalidCell)
						validNeighbors.Add(new GraphConnection(layerPosition, entryCost));
				}
			}
			else
			{
				var layerPosition = new CPos(position.X, position.Y, 0);
				var exitCost = customLayerInfo[posLayer].Layer.ExitMovementCost(actorInfo, locomotorInfo, layerPosition);
				if (exitCost != CostForInvalidCell)
					validNeighbors.Add(new GraphConnection(layerPosition, exitCost));
			}

			return validNeighbors;
		}

		int GetCostToNode(CPos destNode, CVec direction)
		{
			var movementCost = Query.Locomotor.MovementCostToEnterCell(Query.Actor,
				destNode, Query.Check, Query.IgnoreActor);
			var customBlock = Query.CustomBlock;
			if (movementCost != short.MaxValue && !(customBlock != null && customBlock(destNode)))
				return CalculateCellCost(destNode, direction, movementCost);

			return CostForInvalidCell;
		}

		int CalculateCellCost(CPos neighborCPos, CVec direction, int movementCost)
		{
			var cellCost = movementCost;

			if (direction.X * direction.Y != 0)
				cellCost = (cellCost * 34) / 24;

			var customCost = Query.CustomCost;
			if (customCost != null)
			{
				var cc = customCost(neighborCPos);
				if (cc == CostForInvalidCell)
					return CostForInvalidCell;

				cellCost += cc;
			}

			// Prevent units from jumping over height discontinuities
			if (checkTerrainHeight && neighborCPos.Layer == 0)
			{
				var from = neighborCPos - direction;
				var heightLayer = Query.World.Map.Height;
				if (Math.Abs(heightLayer[neighborCPos] - heightLayer[from]) > 1)
					return CostForInvalidCell;
			}

			// Directional bonuses for smoother flow!
			if (laneBias != 0)
			{
				var reverse = (Query.Reverse ? 1 : 0);
				var ux = neighborCPos.X + reverse & 1;
				var uy = neighborCPos.Y + reverse & 1;

				if ((ux == 0 && direction.Y < 0) || (ux == 1 && direction.Y > 0))
					cellCost += laneBias;

				if ((uy == 0 && direction.X < 0) || (uy == 1 && direction.X > 0))
					cellCost += laneBias;
			}

			return cellCost;
		}

		public CellInfo this[CPos pos]
		{
			get => (pos.Layer == 0 ? groundInfo : customLayerInfo[pos.Layer].Info)[pos];
			set => (pos.Layer == 0 ? groundInfo : customLayerInfo[pos.Layer].Info)[pos] = value;
		}

		public void Dispose()
		{
			groundInfo = null;
			customLayerInfo.Clear();
			pooledLayer.Dispose();
		}
	}
}
