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
	public class Fly : IActivity
	{
		public readonly float2 Pos;
		bool isCanceled;

		public Fly(float2 pos) { Pos = pos; }
		public Fly(int2 pos) { Pos = Util.CenterOfCell(pos); }
		
		public IActivity NextActivity { get; set; }
		
		public IActivity Tick(Actor self)
		{
			var cruiseAltitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;

			if (isCanceled) return NextActivity;

			var d = Pos - self.CenterLocation;
			if (d.LengthSquared < 50)		/* close enough */
				return NextActivity;

			var unit = self.traits.Get<Unit>();

			var desiredFacing = Util.GetFacing(d, unit.Facing);
			if (unit.Altitude == cruiseAltitude)
				Util.TickFacing(ref unit.Facing, desiredFacing, 
					self.Info.Traits.Get<UnitInfo>().ROT);

			if (unit.Altitude < cruiseAltitude)
				++unit.Altitude;

			FlyUtil.Fly(self, cruiseAltitude);
			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}

	public static class FlyUtil
	{
		public static void Fly(Actor self, int desiredAltitude )
		{
			var unit = self.traits.Get<Unit>();
			var mobile = self.traits.WithInterface<IMove>().FirstOrDefault();
			var speed = .2f * mobile.MovementSpeedForCell(self, self.Location);
			var angle = unit.Facing / 128f * Math.PI;
			var aircraft = self.traits.Get<Aircraft>();

			self.CenterLocation += speed * -float2.FromAngle((float)angle);
			aircraft.Location = Util.CellContaining(self.CenterLocation);

			unit.Altitude += Math.Sign(desiredAltitude - unit.Altitude);
		}
	}
}
