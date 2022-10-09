#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("General")]
	public class RepairableBuildingProperties : ScriptActorProperties, Requires<RepairableBuildingInfo>
	{
		readonly RepairableBuilding[] repairableBuildings;

		public RepairableBuildingProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			repairableBuildings = self.TraitsImplementing<RepairableBuilding>().ToArray();
		}

		[Desc("Start repairs on this building. `repairer` can be an allied player.")]
		public void StartBuildingRepairs(Player repairer = null)
		{
			repairer = repairer ?? Self.Owner;

			var rb = repairableBuildings.FirstEnabledConditionalTraitOrDefault();
			if (rb != null && !rb.Repairers.Contains(repairer))
				rb.RepairBuilding(Self, repairer);
		}

		[Desc("Stop repairs on this building. `repairer` can be an allied player.")]
		public void StopBuildingRepairs(Player repairer = null)
		{
			repairer = repairer ?? Self.Owner;

			var rb = repairableBuildings.FirstEnabledConditionalTraitOrDefault();
			if (rb != null && rb.RepairActive && rb.Repairers.Contains(repairer))
				rb.RepairBuilding(Self, repairer);
		}
	}
}
