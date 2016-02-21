#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Pathfinder
{
	public static class Constants
	{
		/// <summary>
		/// Min cost to arrive from once cell to an adjacent one
		/// (125 according to runtime tests where we could assess the cost
		/// a unit took to move one cell horizontally)
		/// </summary>
		public const int CellCost = 125;

		/// <summary>
		/// Min cost to arrive from once cell to a diagonal adjacent one
		/// (125 * Sqrt(2) according to runtime tests where we could assess the cost
		/// a unit took to move one cell diagonally)
		/// </summary>
		public const int DiagonalCellCost = 177;

		public const int InvalidNode = int.MaxValue;
	}
}
