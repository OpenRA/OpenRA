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
using OpenRA.GameRules;

namespace OpenRA.Traits.Activities
{
	public class Land : IActivity
	{
		readonly float2 Pos;
		bool isCanceled;
		Actor Structure;
		
		public Land(float2 pos) { Pos = pos; }
		public Land(Actor structure) { Structure = structure; Pos = Structure.CenterLocation; }
		
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (Structure != null && Structure.IsDead)
			{
				Structure = null;
				isCanceled = true;
			}
			
			if (isCanceled) return NextActivity;

			var d = Pos - self.CenterLocation;
			if (d.LengthSquared < 50)		/* close enough */
				return NextActivity;

			var unit = self.traits.Get<Unit>();

			if (unit.Altitude > 0)
				--unit.Altitude;

			var desiredFacing = Util.GetFacing(d, unit.Facing);
			Util.TickFacing(ref unit.Facing, desiredFacing, self.Info.Traits.Get<UnitInfo>().ROT);
			var speed = .2f * Util.GetEffectiveSpeed(self, UnitMovementType.Fly);
			var angle = unit.Facing / 128f * Math.PI;

			self.CenterLocation += speed * -float2.FromAngle((float)angle);
			self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
