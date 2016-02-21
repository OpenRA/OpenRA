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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RepairsUnitsInfo : TraitInfo<RepairsUnits>
	{
		[Desc("Cost in % of the unit value to fully repair the unit.")]
		public readonly int ValuePercentage = 20;
		public readonly int HpPerStep = 10;

		[Desc("Time (in ticks) between two repair steps.")]
		public readonly int Interval = 24;

		[Desc("The sound played when starting to repair a unit.")]
		public readonly string StartRepairingNotification = "Repairing";

		[Desc("The sound played when repairing a unit is done.")]
		public readonly string FinishRepairingNotification = null;
	}

	public class RepairsUnits { }
}
