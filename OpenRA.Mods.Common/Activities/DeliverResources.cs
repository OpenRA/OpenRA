#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	// I haven't renamed the class but this should be called "get into waiting queue"
	public class DeliverResources : Activity
	{
		const int NextChooseTime = 100;

		readonly IMove movement;
		readonly Harvester harv;

		int chosenTicks;

		public DeliverResources(Actor self)
		{
			movement = self.Trait<IMove>();
			harv = self.Trait<Harvester>();
			IsInterruptible = false;
		}

		public override Activity Tick(Actor self)
		{
			// If a refinery is explicitly specified, link it.
			if (harv.OwnerLinkedProc != null && harv.OwnerLinkedProc.IsInWorld)
			{
				harv.LinkProc(self, harv.OwnerLinkedProc);
				harv.OwnerLinkedProc = null;
			}
			// at this point, harv.OwnerLinkedProc == null.

			// Is the refinery still alive? If not, link one.
			if (harv.LinkedProc == null || !harv.LinkedProc.IsInWorld)
			{
				// Maybe we lost the owner-linked refinery:
				harv.LinkedProc = null;
				if (self.World.WorldTick - chosenTicks > NextChooseTime)
				{
					harv.ChooseNewProc(self, null);
					chosenTicks = self.World.WorldTick;
				}
			}

			// No refineries exist. Check again after delay defined in Harvester.
			if (harv.LinkedProc == null)
				return ActivityUtils.SequenceActivities(new Wait(harv.Info.SearchForDeliveryBuildingDelay), this);

			var proc = harv.LinkedProc;
			var iao = proc.Trait<IAcceptResources>();

			self.SetTargetLine(Target.FromActor(proc), Color.Green, false);
			iao.ReserveDock(self, this); // MUST cache this, docks are randomly picked and subject to occupied check.
			return NextActivity;
		}
	}
}
