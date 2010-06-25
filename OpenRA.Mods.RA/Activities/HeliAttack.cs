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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class HeliAttack : IActivity
	{
		Actor target;
		public HeliAttack( Actor target ) { this.target = target; }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead)
				return NextActivity;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
			{	
				self.QueueActivity(new HeliReturn());
				return NextActivity;
			}
			var unit = self.traits.Get<Unit>();
			var aircraft = self.traits.Get<Aircraft>();

			var info = self.Info.Traits.Get<HelicopterInfo>();
			if (unit.Altitude != info.CruiseAltitude)
			{
				unit.Altitude += Math.Sign(info.CruiseAltitude - unit.Altitude);
				return this;
			}

			var range = self.GetPrimaryWeapon().Range - 1;
			var dist = target.CenterLocation - self.CenterLocation;

			var desiredFacing = Util.GetFacing(dist, unit.Facing);
			Util.TickFacing(ref unit.Facing, desiredFacing, self.Info.Traits.Get<UnitInfo>().ROT);

			var mobile = self.traits.WithInterface<IMove>().FirstOrDefault();
			var rawSpeed = .2f * mobile.MovementSpeedForCell(self, self.Location);
			
			if (!float2.WithinEpsilon(float2.Zero, dist, range * Game.CellSize))
				self.CenterLocation += (rawSpeed / dist.Length) * dist;

			return this;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
