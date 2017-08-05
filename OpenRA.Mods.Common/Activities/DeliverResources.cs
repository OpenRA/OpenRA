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
				proc.Trait<DockManager>().ReserveDock(proc, self, this);
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
			var moveToDock = DockUtils.GenericApproachDockActivities(host, client, dock, this);
			Activity extraActivities = null;

			var notify = client.TraitsImplementing<INotifyHarvesterAction>();
			foreach (var n in notify)
			{
				var extra = n.MovingToRefinery(client, dock.Location, moveToDock);

				// We have multiple MovingToRefinery actions to do!
				// Don't know which one to perform.
				if (extra != null)
				{
					if (extraActivities != null)
						throw new InvalidOperationException("Actor {0} has conflicting activities to perform for INotifyHarvesterAction.".F(client.ToString()));

					extraActivities = extra;
				}
			}

			if (extraActivities != null)
				return extraActivities;

			return moveToDock;
		}

		public Activity DockActivities(Actor host, Actor client, Dock dock)
		{
			return host.Trait<Refinery>().DockSequence(client, host, dock);
		}

		Activity IDockActivity.ActivitiesAfterDockDone(Actor host, Actor client, Dock dock)
		{
			// Move to south of the ref to avoid cluttering up with other dock locations
			return new CallFunc(() => harv.ContinueHarvesting(client));
		}

		Activity IDockActivity.ActivitiesOnDockFail(Actor client)
		{
			// go to somewhere else
			return new CallFunc(() => harv.ContinueHarvesting(client));
		}
	}
}
