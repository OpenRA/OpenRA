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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// A dense pathfinding graph that supports a search over all cells within a <see cref="Grid"/>.
	/// Cells outside the grid area are deemed unreachable and will not be considered.
	/// It implements the ability to cost and get connections for cells, and supports <see cref="ICustomMovementLayer"/>.
	/// </summary>
	sealed class GridPathGraph : DensePathGraph
	{
		readonly CellInfo[] infos;
		readonly Grid grid;

		public GridPathGraph(Locomotor locomotor, Actor actor, World world, BlockedByActor check,
			Func<CPos, int> customCost, Actor ignoreActor, bool laneBias, bool inReverse, Grid grid)
			: base(locomotor, actor, world, check, customCost, ignoreActor, laneBias, inReverse)
		{
			infos = new CellInfo[grid.Width * grid.Height];
			this.grid = grid;
		}

		protected override bool IsValidNeighbor(CPos neighbor)
		{
			// Enforce that we only search within the grid bounds.
			return grid.Contains(neighbor);
		}

		int InfoIndex(CPos pos)
		{
			return
				(pos.Y - grid.TopLeft.Y) * grid.Width +
				(pos.X - grid.TopLeft.X);
		}

		public override CellInfo this[CPos pos]
		{
			get => infos[InfoIndex(pos)];
			set => infos[InfoIndex(pos)] = value;
		}
	}
}
