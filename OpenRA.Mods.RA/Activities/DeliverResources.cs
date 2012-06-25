#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Traits;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Activities
{
	public class DeliverResources : Activity
	{
		bool isDocking;
		int chosenTicks;

		const int NextChooseTime = 100;

		public DeliverResources() { }

		public override Activity Tick(Actor self)
		{
			if (NextActivity != null)
				return NextActivity;

			var mobile = self.Trait<Mobile>();
			var harv = self.Trait<Harvester>();

			// Find the nearest best refinery if not explicitly ordered to a specific refinery:
			if (harv.OwnerLinkedProc == null || !harv.OwnerLinkedProc.IsInWorld)
			{
				// Maybe we lost the owner-linked refinery:
				harv.OwnerLinkedProc = null;
				if (self.World.FrameNumber - chosenTicks > NextChooseTime)
				{
					harv.ChooseNewProc(self, null);
					chosenTicks = self.World.FrameNumber;
				}
			}
			else
			{
				harv.LinkProc(self, harv.OwnerLinkedProc);
			}

			if (harv.LinkedProc == null || !harv.LinkedProc.IsInWorld)
				harv.ChooseNewProc(self, null);

			if (harv.LinkedProc == null)	// no procs exist; check again in 1s.
				return Util.SequenceActivities(new Wait(25), this);

			var proc = harv.LinkedProc;
			var iao = proc.Trait<IAcceptOre>();

			self.SetTargetLine(Target.FromActor(proc), Color.Green, false);
			if (self.Location != proc.Location + iao.DeliverOffset)
				return Util.SequenceActivities(mobile.MoveTo(proc.Location + iao.DeliverOffset, 0), this);

			if (!isDocking)
			{
				isDocking = true;
				iao.OnDock(self, this);
			}

			return Util.SequenceActivities(new Wait(10), this);
		}

		// Cannot be cancelled
		public override void Cancel(Actor self) { }
	}
}
