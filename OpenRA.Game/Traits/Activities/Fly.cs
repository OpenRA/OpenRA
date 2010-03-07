#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;

namespace OpenRA.Traits.Activities
{
	public class Fly : IActivity
	{
		readonly float2 Pos;
		bool isCanceled;

		public Fly(float2 pos) { Pos = pos; }
		public Fly(int2 pos) { Pos = Util.CenterOfCell(pos); }
		
		public IActivity NextActivity { get; set; }

		const int CruiseAltitude = 20;
		
		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;

			var d = Pos - self.CenterLocation;
			if (d.LengthSquared < 50)		/* close enough */
				return NextActivity;

			var unit = self.traits.Get<Unit>();

			var desiredFacing = Util.GetFacing(d, unit.Facing);
			if (unit.Altitude == CruiseAltitude)
				Util.TickFacing(ref unit.Facing, desiredFacing, 
					self.Info.Traits.Get<UnitInfo>().ROT);
			
			if (unit.Altitude < CruiseAltitude)
				++unit.Altitude;

			FlyUtil.Fly(self, CruiseAltitude);
			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}

	public static class FlyUtil
	{
		public static void Fly(Actor self, int desiredAltitude )
		{
			var unit = self.traits.Get<Unit>();
			var speed = .2f * Util.GetEffectiveSpeed(self);
			var angle = unit.Facing / 128f * Math.PI;

			self.CenterLocation += speed * -float2.FromAngle((float)angle);
			self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();

			unit.Altitude += Math.Sign(desiredAltitude - unit.Altitude);
		}
	}
}
