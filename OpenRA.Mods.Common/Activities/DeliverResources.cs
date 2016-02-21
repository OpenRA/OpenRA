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

		bool isDocking;
		int chosenTicks;

		public DeliverResources(Actor self)
		{
			movement = self.Trait<IMove>();
			harv = self.Trait<Harvester>();
		}

		public override Activity Tick(Actor self)
		{
			if (NextActivity != null)
				return NextActivity;

			// Find the nearest best refinery if not explicitly ordered to a specific refinery:
			if (harv.OwnerLinkedProc == null || !harv.OwnerLinkedProc.IsInWorld)
			{
				// Maybe we lost the owner-linked refinery:
				harv.OwnerLinkedProc = null;
				if (self.World.WorldTick - chosenTicks > NextChooseTime)
				{
					harv.ChooseNewProc(self, null);
					chosenTicks = self.World.WorldTick;
				}
			}
			else
				harv.LinkProc(self, harv.OwnerLinkedProc);

			if (harv.LinkedProc == null || !harv.LinkedProc.IsInWorld)
				harv.ChooseNewProc(self, null);

			// No refineries exist; check again after delay defined in Harvester.
			if (harv.LinkedProc == null)
				return ActivityUtils.SequenceActivities(new Wait(harv.Info.SearchForDeliveryBuildingDelay), this);

			var proc = harv.LinkedProc;
			var iao = proc.Trait<IAcceptResources>();

			self.SetTargetLine(Target.FromActor(proc), Color.Green, false);
			if (self.Location != proc.Location + iao.DeliveryOffset)
			{
				var notify = self.TraitsImplementing<INotifyHarvesterAction>();
				var next = new DeliverResources(self);
				foreach (var n in notify)
					n.MovingToRefinery(self, proc.Location + iao.DeliveryOffset, next);

				return ActivityUtils.SequenceActivities(movement.MoveTo(proc.Location + iao.DeliveryOffset, 0), this);
			}

			if (!isDocking)
			{
				isDocking = true;
				iao.OnDock(self, this);
			}

			return ActivityUtils.SequenceActivities(new Wait(10), this);
		}

		// Cannot be cancelled
		public override void Cancel(Actor self) { }
	}
}
