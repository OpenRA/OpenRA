﻿#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	using OccupiedCells = IEnumerable<Pair<CPos, SubCell>>;

	public static class ShroudExts
	{
		public static bool AnyExplored(this Shroud shroud, OccupiedCells cells)
		{
			foreach (var cell in cells)
				if (shroud.IsExplored(cell.First))
					return true;

			return false;
		}

		public static bool AnyVisible(this Shroud shroud, OccupiedCells cells)
		{
			foreach (var cell in cells)
				if (shroud.IsVisible(cell.First))
					return true;

			return false;
		}
	}
}
