#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	class HeliFly : Activity
	{
		public readonly PPos Dest;
		public HeliFly(PPos dest)
		{
			Dest = dest;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)	return NextActivity;

			var info = self.Info.Traits.Get<HelicopterInfo>();
			var aircraft = self.Trait<Aircraft>();

			if (aircraft.Altitude != info.CruiseAltitude)
			{
				aircraft.Altitude += Math.Sign(info.CruiseAltitude - aircraft.Altitude);
				return this;
			}

			var dist = Dest - aircraft.PxPosition;
			if (Math.Abs(dist.X) < 2 && Math.Abs(dist.Y) < 2)
			{
				aircraft.SubPxPosition = Dest.ToPSubPos();
				return NextActivity;
			}

			var desiredFacing = Util.GetFacing(dist, aircraft.Facing);
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.ROT);
			aircraft.TickMove( PSubPos.PerPx * aircraft.MovementSpeed, desiredFacing );

			return this;
		}

		public override IEnumerable<Target> GetTargets( Actor self )
		{
			yield return Target.FromPos(Dest);
		}
	}
}
