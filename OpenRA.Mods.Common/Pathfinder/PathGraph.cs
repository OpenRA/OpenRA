#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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

	public struct GraphConnection
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
		public Actor Actor { get; private set; }
		public World World { get; private set; }
		public Func<CPos, bool> CustomBlock { get; set; }
		public Func<CPos, int> CustomCost { get; set; }
		public int LaneBias { get; set; }
		public bool InReverse { get; set; }
		public Actor IgnoreActor { get; set; }

		readonly CellConditions checkConditions;
		readonly MobileInfo mobileInfo;
		readonly MobileInfo.WorldMovementInfo worldMovementInfo;
		readonly CellInfoLayerPool.PooledCellInfoLayer pooledLayer;
		readonly bool checkTerrainHeight;
		CellLayer<CellInfo> groundInfo;

		readonly Dictionary<byte, Pair<ICustomMovementLayer, CellLayer<CellInfo>>> customLayerInfo =
			new Dictionary<byte, Pair<ICustomMovementLayer, CellLayer<CellInfo>>>();

		public PathGraph(CellInfoLayerPool layerPool, MobileInfo mobileInfo, Actor actor, World world, bool checkForBlocked)
		{
			pooledLayer = layerPool.Get();
			groundInfo = pooledLayer.GetLayer();
			var layers = world.GetCustomMovementLayers().Values
				.Where(cml => cml.EnabledForActor(actor.Info, mobileInfo));

			foreach (var cml in layers)
				customLayerInfo[cml.Index] = Pair.New(cml, pooledLayer.GetLayer());

			World = world;
			this.mobileInfo = mobileInfo;
			worldMovementInfo = mobileInfo.GetWorldMovementInfo(world);
			Actor = actor;
			LaneBias = 1;
			checkConditions = checkForBlocked ? CellConditions.TransientActors : CellConditions.None;
			checkTerrainHeight = world.Map.Grid.MaximumTerrainHeight > 0;
		}

		// Sets of neighbors for each incoming direction. These exclude the neighbors which are guaranteed
		// to be reached more cheaply by a path through our parent cell which does not include the current cell.
		// For horizontal/vertical directions, the set is the three cells 'ahead'. For diagonal directions, the set
		// is the three cells ahead, plus the two cells to the side, which we cannot exclude without knowing if
		// the cell directly between them and our parent is passable.
		static readonly CVec[][] DirectedNeighbors = {
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
			var info = position.Layer == 0 ? groundInfo : customLayerInfo[position.Layer].Second;
			var previousPos = info[position].PreviousPos;

			var dx = position.X - previousPos.X;
			var dy = position.Y - previousPos.Y;
			var index = dy * 3 + dx + 4;

			var directions = DirectedNeighbors[index];
			var validNeighbors = new List<GraphConnection>(directions.Length);
			for (var i = 0; i < directions.Length; i++)
			{
				var neighbor = position + directions[i];
				var movementCost = GetCostToNode(neighbor, directions[i]);
				if (movementCost != Constants.InvalidNode)
					validNeighbors.Add(new GraphConnection(neighbor, movementCost));
			}

			if (position.Layer == 0)
			{
				foreach (var cli in customLayerInfo.Values)
				{
					var layerPosition = new CPos(position.X, position.Y, cli.First.Index);
					var entryCost = cli.First.EntryMovementCost(Actor.Info, mobileInfo, layerPosition);
					if (entryCost != Constants.InvalidNode)
						validNeighbors.Add(new GraphConnection(layerPosition, entryCost));
				}
			}
			else
			{
				var layerPosition = new CPos(position.X, position.Y, 0);
				var exitCost = customLayerInfo[position.Layer].First.ExitMovementCost(Actor.Info, mobileInfo, layerPosition);
				if (exitCost != Constants.InvalidNode)
					validNeighbors.Add(new GraphConnection(layerPosition, exitCost));
			}

			return validNeighbors;
		}

		int GetCostToNode(CPos destNode, CVec direction)
		{
			var movementCost = mobileInfo.MovementCostToEnterCell(worldMovementInfo, Actor, destNode, IgnoreActor, checkConditions);
			if (movementCost != int.MaxValue && !(CustomBlock != null && CustomBlock(destNode)))
				return CalculateCellCost(destNode, direction, movementCost);

			return Constants.InvalidNode;
		}

		int CalculateCellCost(CPos neighborCPos, CVec direction, int movementCost)
		{
			var cellCost = movementCost;

			if (direction.X * direction.Y != 0)
				cellCost = (cellCost * 34) / 24;

			if (CustomCost != null)
			{
				var customCost = CustomCost(neighborCPos);
				if (customCost == Constants.InvalidNode)
					return Constants.InvalidNode;

				cellCost += customCost;
			}

			// Prevent units from jumping over height discontinuities
			if (checkTerrainHeight && neighborCPos.Layer == 0)
			{
				var from = neighborCPos - direction;
				if (Math.Abs(World.Map.Height[neighborCPos] - World.Map.Height[from]) > 1)
					return Constants.InvalidNode;
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
			get { return (pos.Layer == 0 ? groundInfo : customLayerInfo[pos.Layer].Second)[pos]; }
			set { (pos.Layer == 0 ? groundInfo : customLayerInfo[pos.Layer].Second)[pos] = value; }
		}

		public void Dispose()
		{
			groundInfo = null;
			customLayerInfo.Clear();
			pooledLayer.Dispose();
		}
	}
}
