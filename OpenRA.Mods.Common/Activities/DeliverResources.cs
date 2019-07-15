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

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			// Find the nearest best refinery if not explicitly ordered to a specific refinery:
			if (harv.LinkedProc == null || !harv.LinkedProc.IsInWorld)
				harv.ChooseNewProc(self, null);

			// No refineries exist; check again after delay defined in Harvester.
			if (harv.LinkedProc == null)
			{
				QueueChild(new Wait(harv.Info.SearchForDeliveryBuildingDelay));
				return false;
			}

			var proc = harv.LinkedProc;
			var iao = proc.Trait<IAcceptResources>();

			self.SetTargetLine(Target.FromActor(proc), Color.Green, false);
			if (self.Location != proc.Location + iao.DeliveryOffset)
			{
				foreach (var n in self.TraitsImplementing<INotifyHarvesterAction>())
					n.MovingToRefinery(self, proc);

				QueueChild(movement.MoveTo(proc.Location + iao.DeliveryOffset, 0));
				return false;
			}

			QueueChild(new Wait(10));
			iao.OnDock(self, this);
			return true;
		}
	}
}
