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

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	// I haven't renamed the class but this should be called "get into waiting queue"
	public class DeliverResources : Activity, IDockActivity
	{
		const int NextChooseTime = 100;

		readonly Harvester harv;

		int chosenTicks;

		public DeliverResources(Actor self)
		{
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
			//// at this point, harv.OwnerLinkedProc == null.

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
			{
				Queue(ActivityUtils.SequenceActivities(new Wait(harv.Info.SearchForDeliveryBuildingDelay), new DeliverResources(self)));
				return NextActivity;
			}

			var proc = harv.LinkedProc;
			self.SetTargetLine(Target.FromActor(proc), Color.Green, false);

			if (!self.Info.TraitInfo<HarvesterInfo>().OreTeleporter)
			{
				proc.Trait<DockManager>().ReserveDock(proc, self, this);
			}
			else
			{
				var dock = proc.TraitsImplementing<Dock>().First();
				Queue(DockActivities(proc, self, dock));
				Queue(new CallFunc(() => harv.ContinueHarvesting(self)));
			}

			return NextActivity;
		}

		Activity IDockActivity.ApproachDockActivities(Actor host, Actor client, Dock dock)
		{
			return DockUtils.GenericApproachDockActivities(host, client, dock, this);
		}

		public Activity DockActivities(Actor host, Actor client, Dock dock)
		{
			return host.Trait<Refinery>().DockSequence(client, host, dock);
		}

		Activity IDockActivity.ActivitiesAfterDockDone(Actor host, Actor client, Dock dock)
		{
			// Move to south of the ref to avoid cluttering up with other dock locations
			return ActivityUtils.SequenceActivities(
				client.Trait<IMove>().MoveTo(dock.Location + dock.Info.ExitOffset, 2),
				new CallFunc(() => harv.ContinueHarvesting(client)));
		}

		Activity IDockActivity.ActivitiesOnDockFail(Actor client)
		{
			// go to somewhere else
			return new CallFunc(() => harv.ContinueHarvesting(client));
		}
	}
}
