#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class HeliFly : IActivity
	{
		public readonly float2 Dest;
		public HeliFly(float2 dest)
		{
			Dest = dest;
		}

		public IActivity NextActivity { get; set; }
		bool isCanceled;
		
		public IActivity Tick(Actor self)
		{
			if (isCanceled)
				return NextActivity;

			var unit = self.traits.Get<Unit>();
			var info = self.Info.Traits.Get<HelicopterInfo>();
			var aircraft = self.traits.Get<Aircraft>();

			if (unit.Altitude != info.CruiseAltitude)
			{
				unit.Altitude += Math.Sign(info.CruiseAltitude - unit.Altitude);
				return this;
			}
			
			var dist = Dest - self.CenterLocation;
			if (float2.WithinEpsilon(float2.Zero, dist, 2))
			{
				self.CenterLocation = Dest;
				aircraft.Location = Util.CellContaining(self.CenterLocation);
				return NextActivity;
			}

			var desiredFacing = Util.GetFacing(dist, unit.Facing);
			Util.TickFacing(ref unit.Facing, desiredFacing, 
				self.Info.Traits.Get<UnitInfo>().ROT);

			var mobile = self.traits.WithInterface<IMove>().FirstOrDefault();
			var rawSpeed = .2f * mobile.MovementSpeedForCell(self, self.Location);
			self.CenterLocation += (rawSpeed / dist.Length) * dist;
			aircraft.Location = Util.CellContaining(self.CenterLocation);

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
