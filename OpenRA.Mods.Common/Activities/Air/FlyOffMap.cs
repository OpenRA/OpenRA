#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyOffMap : Activity
	{
		readonly Aircraft aircraft;
		readonly Target target;
		readonly bool hasTarget;
		int endingDelay;

		public FlyOffMap(Actor self, int endingDelay = 25)
		{
			aircraft = self.Trait<Aircraft>();
			ChildHasPriority = false;
			this.endingDelay = endingDelay;
		}

		public FlyOffMap(Actor self, in Target target, int endingDelay = 25)
			: this(self, endingDelay)
		{
			this.target = target;
			hasTarget = true;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (hasTarget)
			{
				QueueChild(new Fly(self, target));
				QueueChild(new FlyForward(self));
				return;
			}

			// VTOLs must take off first if they're not at cruise altitude
			if (aircraft.Info.VTOL && self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition) != aircraft.Info.CruiseAltitude)
				QueueChild(new TakeOff(self));

			QueueChild(new FlyForward(self));
		}

		public override bool Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
				Cancel(self);

			if (IsCanceling)
				return true;

			if (!self.World.Map.Contains(self.Location) && --endingDelay < 0)
				ChildActivity.Cancel(self);

			return TickChild(self);
		}
	}
}
