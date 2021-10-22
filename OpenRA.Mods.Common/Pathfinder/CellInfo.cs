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

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// Describes the three states that a node in the graph can have.
	/// Based on A* algorithm specification
	/// </summary>
	public enum CellStatus : byte
	{
		Unvisited,
		Open,
		Closed
	}

	/// <summary>
	/// Stores information about nodes in the pathfinding graph.
	/// The default value of this struct represents an <see cref="CellStatus.Unvisited"/> location.
	/// </summary>
	public readonly struct CellInfo
	{
		/// <summary>
		/// The cost to move from the start up to this node.
		/// </summary>
		public readonly int CostSoFar;

		/// <summary>
		/// The estimation of how far this node is from our target.
		/// </summary>
		public readonly int EstimatedTotal;

		/// <summary>
		/// The previous node of this one that follows the shortest path.
		/// </summary>
		public readonly CPos PreviousPos;

		/// <summary>
		/// The status of this node. Accessing other fields is only valid when the status is not <see cref="CellStatus.Unvisited"/>.
		/// </summary>
		public readonly CellStatus Status;

		public CellInfo(int costSoFar, int estimatedTotal, CPos previousPos, CellStatus status)
		{
			if (status == CellStatus.Unvisited)
				throw new ArgumentException(
					$"The default {nameof(CellInfo)} is the only such {nameof(CellInfo)} allowed for representing an {nameof(CellStatus.Unvisited)} location.",
					nameof(status));

			Status = status;
			CostSoFar = costSoFar;
			EstimatedTotal = estimatedTotal;
			PreviousPos = previousPos;
		}

		public override string ToString()
		{
			if (Status == CellStatus.Unvisited)
				return Status.ToString();

			return
				$"{Status} {nameof(CostSoFar)}={CostSoFar} " +
				$"{nameof(EstimatedTotal)}={EstimatedTotal} {nameof(PreviousPos)}={PreviousPos}";
		}
	}
}
