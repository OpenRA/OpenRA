#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;

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
		ICollection<GraphConnection> GetConnections(CPos position);

		/// <summary>
		/// Retrieves an object given a node in the graph
		/// </summary>
		T this[CPos pos] { get; set; }

		Func<CPos, bool> CustomBlock { get; set; }

		Func<CPos, int> CustomCost { get; set; }

		int LaneBias { get; set; }

		bool InReverse { get; set; }

		Actor IgnoredActor { get; set; }

		World World { get; }

		Actor Actor { get; }
	}

	public struct GraphConnection
	{
		public readonly CPos Source;
		public readonly CPos Destination;
		public readonly int Cost;

		public GraphConnection(CPos source, CPos destination, int cost)
		{
			Source = source;
			Destination = destination;
			Cost = cost;
		}
	}

	public class PathGraph : IGraph<CellInfo>
	{
		public Actor Actor { get; private set; }
		public World World { get; private set; }
		public Func<CPos, bool> CustomBlock { get; set; }
		public Func<CPos, int> CustomCost { get; set; }
		public int LaneBias { get; set; }
		public bool InReverse { get; set; }
		public Actor IgnoredActor { get; set; }

		readonly CellConditions checkConditions;
		readonly MobileInfo mobileInfo;
		CellLayer<CellInfo> cellInfo;

		public const int InvalidNode = int.MaxValue;

		public PathGraph(CellLayer<CellInfo> cellInfo, MobileInfo mobileInfo, Actor actor, World world, bool checkForBlocked)
		{
			this.cellInfo = cellInfo;
			World = world;
			this.mobileInfo = mobileInfo;
			Actor = actor;
			LaneBias = 1;
			checkConditions = checkForBlocked ? CellConditions.TransientActors : CellConditions.None;
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

		public ICollection<GraphConnection> GetConnections(CPos position)
		{
			var previousPos = cellInfo[position].PreviousPos;

			var dx = position.X - previousPos.X;
			var dy = position.Y - previousPos.Y;
			var index = dy * 3 + dx + 4;

			var validNeighbors = new LinkedList<GraphConnection>();
			var directions = DirectedNeighbors[index];
			for (var i = 0; i < directions.Length; i++)
			{
				var neighbor = position + directions[i];
				var movementCost = GetCostToNode(neighbor, directions[i]);
				if (movementCost != InvalidNode)
					validNeighbors.AddLast(new GraphConnection(position, neighbor, movementCost));
			}

			return validNeighbors;
		}

		int GetCostToNode(CPos destNode, CVec direction)
		{
			int movementCost;
			if (mobileInfo.CanEnterCell(
				World as World,
				Actor as Actor,
				destNode,
				out movementCost,
				IgnoredActor as Actor,
				checkConditions) && !(CustomBlock != null && CustomBlock(destNode)))
			{
				return CalculateCellCost(destNode, direction, movementCost);
			}

			return InvalidNode;
		}

		int CalculateCellCost(CPos neighborCPos, CVec direction, int movementCost)
		{
			var cellCost = movementCost;

			if (direction.X * direction.Y != 0)
				cellCost = (cellCost * 34) / 24;

			if (CustomCost != null)
				cellCost += CustomCost(neighborCPos);

			// directional bonuses for smoother flow!
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

		bool disposed;
		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;

			CellInfoLayerManager.Instance.PutBackIntoPool(cellInfo);
			cellInfo = null;

			GC.SuppressFinalize(this);
		}

		~PathGraph() { Dispose(); }

		public CellInfo this[CPos pos]
		{
			get
			{
				return cellInfo[pos];
			}

			set
			{
				cellInfo[pos] = value;
			}
		}
	}
}
