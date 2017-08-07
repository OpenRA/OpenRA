#region Copyright & License Information
/*
 * Written by Boolbada of OP Mod
 * Follows GPLv3 License as the OpenRA engine:
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
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Yupgi_alert.Activities
{
	public class OpportunityTeleport : Activity
	{
		readonly PortableChronoInfo pchronoInfo;
		readonly PortableChrono pchrono;
		readonly CPos targetCell;

		// moveToDest: activities that will make this actor move to the destination.
		// i.e., Move.
		public OpportunityTeleport(Actor self, PortableChronoInfo pchronoInfo, CPos targetCell, Activity moveToDest)
		{
			this.pchronoInfo = pchronoInfo;
			pchrono = self.Trait<PortableChrono>();
			this.targetCell = targetCell;
			QueueChild(moveToDest);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextInQueue;

			// Arrived, one way or another.
			if (ChildActivity == null)
				return NextInQueue;

			if (pchrono.CanTeleport && (self.Location - targetCell).LengthSquared > 4)
			{
				ChildActivity = new Teleport(self, targetCell, null,
					pchronoInfo.KillCargo, pchronoInfo.FlashScreen, pchronoInfo.ChronoshiftSound);

				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
			return this;
		}
	}
}
