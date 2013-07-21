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
		readonly WPos pos;
		public HeliFly(WPos pos) { this.pos = pos; }
		public HeliFly(CPos pos) { this.pos = pos.CenterPosition; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var info = self.Info.Traits.Get<HelicopterInfo>();
			var aircraft = self.Trait<Aircraft>();

			if (aircraft.Altitude != info.CruiseAltitude)
			{
				aircraft.Altitude += Math.Sign(info.CruiseAltitude - aircraft.Altitude);
				return this;
			}

			// The next move would overshoot, so just set the final position
			var dist = pos - self.CenterPosition;
			var moveDist = aircraft.MovementSpeed * 7 * 1024 / (Game.CellSize * 32);
			if (dist.HorizontalLengthSquared < moveDist*moveDist)
			{
				aircraft.SetPosition(self, pos);
				return NextActivity;
			}

			var desiredFacing = Util.GetFacing(dist, aircraft.Facing);
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.ROT);
			aircraft.TickMove(PSubPos.PerPx * aircraft.MovementSpeed, desiredFacing);

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(pos);
		}
	}
}
