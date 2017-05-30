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
	public class DeliverResources : Activity
	{
		const int NextChooseTime = 100;

		readonly IMove movement;
		readonly Harvester harv;

		bool isDocking = false;
		int chosenTicks;

		public DeliverResources(Actor self)
		{
			movement = self.Trait<IMove>();
			harv = self.Trait<Harvester>();
			IsInterruptible = false;
		}

		bool teleporter_bug_fix_ran_once = false;
		public override Activity Tick(Actor self)
		{
			if (NextInQueue != null)
				return NextInQueue;

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

			// No refineries exist; check again after delay defined in Harvester.
			if (harv.LinkedProc == null)
				return ActivityUtils.SequenceActivities(new Wait(harv.Info.SearchForDeliveryBuildingDelay), this);

			var proc = harv.LinkedProc;
			var iao = proc.Trait<IAcceptResources>();

			self.SetTargetLine(Target.FromActor(proc), Color.Green, false);
			var dest = proc.Location + iao.DeliveryOffset;
			if (harv.Info.OreTeleporter)
			{
				// do nothing and continue below.
				// Force issue dock order.
				if (!teleporter_bug_fix_ran_once)
				{
					teleporter_bug_fix_ran_once = true;
					return ActivityUtils.SequenceActivities(new Wait(10), this);
				}
			}
			else if (self.Location != dest)
			{
				var notify = self.TraitsImplementing<INotifyHarvesterAction>();
				foreach (var n in notify)
					n.MovingToRefinery(self, dest, this);

				// Move to the target proc then came back to this activity for re-eval, I think.
				return ActivityUtils.SequenceActivities(movement.MoveTo(dest, 0), this);
			}

			if (!isDocking)
			{
				isDocking = true;
				iao.OnDock(self, this);

				// Run what OnDock queued.
				return NextActivity;
			}

			return ActivityUtils.SequenceActivities(new Wait(10), this);
		}
	}
}
