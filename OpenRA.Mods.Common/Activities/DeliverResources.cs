#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class DeliverResources : Activity
	{
		readonly IMove movement;
		readonly Harvester harv;
		readonly INotifyHarvesterAction[] notifyHarvesterActions;
		IAcceptResources proc;

		public DeliverResources(Harvester harv, IAcceptResources proc = null)
		{
			this.harv = harv;
			this.proc = proc;
			movement = harv.Self.Trait<IMove>();
			notifyHarvesterActions = harv.Self.TraitsImplementing<INotifyHarvesterAction>().ToArray();
		}

		protected override void OnFirstRun(Actor self)
		{
			if (proc != null && proc.IsAliveAndInWorld)
				harv.LinkProc(proc);
		}

		public override bool Tick(Actor self)
		{
			if (harv.IsTraitDisabled)
				Cancel(self, true);

			if (IsCanceling)
				return true;

			// Find the nearest best refinery if not explicitly ordered to a specific refinery:
			if (proc == null || harv.LinkedProc == null)
				proc = harv.ChooseNewProc(null);

			// No refineries exist; check again after delay defined in Harvester.
			if (proc == null || harv.LinkedProc == null)
			{
				QueueChild(new Wait(harv.Info.SearchForDeliveryBuildingDelay));
				return false;
			}

			if (self.Location != proc.Location)
			{
				foreach (var n in notifyHarvesterActions)
					n.MovingToRefinery(self, proc);

				QueueChild(movement.MoveTo(proc.Location, 0));
				return false;
			}

			QueueChild(new Wait(10));
			proc.OnDock(harv, this);
			return true;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			foreach (var n in notifyHarvesterActions)
				n.MovementCancelled(self);

			base.Cancel(self, keepQueue);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (proc != null)
				yield return new TargetLineNode(Target.FromActor(proc.Self), harv.Info.DeliverLineColor);
			else
				yield return new TargetLineNode(Target.FromActor(harv.LinkedProc?.Self), harv.Info.DeliverLineColor);
		}
	}
}
