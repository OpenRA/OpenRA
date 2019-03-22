#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class DeliverResources : Activity
	{
		readonly IMove movement;
		readonly Harvester harv;
		readonly Actor targetActor;

		bool isDocking;

		public DeliverResources(Actor self, Actor targetActor = null)
		{
			movement = self.Trait<IMove>();
			harv = self.Trait<Harvester>();
			this.targetActor = targetActor;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (targetActor != null && targetActor.IsInWorld)
				harv.LinkProc(self, targetActor);
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (IsCanceling || isDocking)
				return NextActivity;

			// Find the nearest best refinery if not explicitly ordered to a specific refinery:
			if (harv.LinkedProc == null || !harv.LinkedProc.IsInWorld)
				harv.ChooseNewProc(self, null);

			// No refineries exist; check again after delay defined in Harvester.
			if (harv.LinkedProc == null)
			{
				QueueChild(self, new Wait(harv.Info.SearchForDeliveryBuildingDelay), true);
				return this;
			}

			var proc = harv.LinkedProc;
			var iao = proc.Trait<IAcceptResources>();

			self.SetTargetLine(Target.FromActor(proc), Color.Green, false);
			if (self.Location != proc.Location + iao.DeliveryOffset)
			{
				foreach (var n in self.TraitsImplementing<INotifyHarvesterAction>())
					n.MovingToRefinery(self, proc, new FindAndDeliverResources(self));

				QueueChild(self, movement.MoveTo(proc.Location + iao.DeliveryOffset, 0), true);
				return this;
			}

			if (!isDocking)
			{
				QueueChild(self, new Wait(10), true);
				isDocking = true;
				iao.OnDock(self, this);
				return this;
			}

			return NextActivity;
		}
	}
}
