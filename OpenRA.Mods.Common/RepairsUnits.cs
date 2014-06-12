#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class RepairsUnitsInfo : TraitInfo<RepairsUnits>
	{
		public readonly int ValuePercentage = 20; // charge 20% of the unit value to fully repair
		public readonly int HpPerStep = 10;
		public readonly int Interval = 24; // Ticks
	}

	public class RepairsUnits { }
}
