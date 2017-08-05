#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod,
 * Copy-pasted from AutoCarryall.cs in d2k mod.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Yupgi_alert.Activities;
using OpenRA.Traits;

/*
 * Works without base engine modification.
 * 
 * This module implements an OP version of chrono miner.
 * Similar to RA2 chrono miners, harvesters with this trait teleports back to refinary.
 * (Not to the docking position but a near cell).
 * What's OP, is that it teleports back to the resource field!
 */

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Automatically transports harvesters with the Carryable trait between resource fields and refineries.")]
	public class ChronoHarvesterInfo : ConditionalTraitInfo, Requires<PortableChronoInfo>
	{
		public override object Create(ActorInitializer init) { return new ChronoHarvester(init.Self, this); }
	}

	public class ChronoHarvester : INotifyHarvesterAction
	{
		readonly PortableChronoInfo pchronoInfo;

		public ChronoHarvester(Actor self, ChronoHarvesterInfo info)
		{
			pchronoInfo = self.Info.TraitInfo<PortableChronoInfo>();
		}

		void INotifyHarvesterAction.Docked() { }

		void INotifyHarvesterAction.Harvested(Actor self, ResourceType resource) { }

		void INotifyHarvesterAction.MovementCancelled(Actor self) { }

		void INotifyHarvesterAction.Undocked() { }

		Activity INotifyHarvesterAction.MovingToRefinery(Actor self, CPos targetCell, Activity next)
		{
			return new OpportunityTeleport(self, pchronoInfo, targetCell, next);
		}

		Activity INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell, Activity next)
		{
			return new OpportunityTeleport(self, pchronoInfo, targetCell, next);
		}
	}
}
