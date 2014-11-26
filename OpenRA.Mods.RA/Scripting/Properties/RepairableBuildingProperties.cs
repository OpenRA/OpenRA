#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Buildings;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("General")]
	public class RepairableBuildingProperties : ScriptActorProperties, Requires<RepairableBuildingInfo>
	{
		readonly RepairableBuilding rb;

		public RepairableBuildingProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			rb = self.Trait<RepairableBuilding>();
		}

		[Desc("Start repairs on this building. `repairer` can be an allied player.")]
		public void StartBuildingRepairs(Player repairer = null)
		{
			repairer = repairer ?? self.Owner;

			if (!rb.Repairers.Contains(repairer))
				rb.RepairBuilding(self, repairer);
		}

		[Desc("Stop repairs on this building. `repairer` can be an allied player.")]
		public void StopBuildingRepairs(Player repairer = null)
		{
			repairer = repairer ?? self.Owner;

			if (rb.RepairActive && rb.Repairers.Contains(repairer))
				rb.RepairBuilding(self, repairer);
		}
	}
}
