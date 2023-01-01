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
	/// A sparse pathfinding graph that supports a search over provided cells.
	/// This is a classic graph that supports an arbitrary graph of nodes and edges,
	/// and does not require a dense grid of cells.
	/// Costs and any desired connections to a <see cref="Traits.ICustomMovementLayer"/>
	/// must be provided as input.
	/// </summary>
	sealed class SparsePathGraph : IPathGraph
	{
		readonly Func<CPos, List<GraphConnection>> edges;
		readonly Dictionary<CPos, CellInfo> info;

		public SparsePathGraph(Func<CPos, List<GraphConnection>> edges, int estimatedSearchSize = 0)
		{
			this.edges = edges;
			info = new Dictionary<CPos, CellInfo>(estimatedSearchSize);
		}

		public List<GraphConnection> GetConnections(CPos position)
		{
			return edges(position) ?? new List<GraphConnection>();
		}

		public CellInfo this[CPos pos]
		{
			get
			{
				if (info.TryGetValue(pos, out var cellInfo))
					return cellInfo;
				return default;
			}
			set => info[pos] = value;
		}

		public void Dispose() { }
	}
}
