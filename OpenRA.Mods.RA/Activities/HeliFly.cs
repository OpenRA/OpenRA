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
using OpenRA.Traits;
using System.Linq;

namespace OpenRA.Mods.RA.Activities
{
	class HeliFly : IActivity
	{
		readonly float2 Dest;
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

			if (unit.Altitude != info.CruiseAltitude)
			{
				unit.Altitude += Math.Sign(info.CruiseAltitude - unit.Altitude);
				return this;
			}
			
			// Prevent multiple units from stacking together
			var otherHelis = self.World.FindUnitsInCircle(self.CenterLocation, info.IdealSeparation)
				.Where(a => a.traits.Contains<Helicopter>());
			
			var f = otherHelis
				.Select(h => GetRepulseForce(self, h))
				.Aggregate(float2.Zero, (a, b) => a + b);
			
			var dist = Dest - self.CenterLocation + f;
			if (float2.WithinEpsilon(float2.Zero, dist, 2))
			{
				self.CenterLocation = Dest;
				self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();
				return NextActivity;
			}

			var desiredFacing = Util.GetFacing(dist, unit.Facing);
			Util.TickFacing(ref unit.Facing, desiredFacing, 
				self.Info.Traits.Get<UnitInfo>().ROT);

			var rawSpeed = .2f * Util.GetEffectiveSpeed(self, UnitMovementType.Fly);
			self.CenterLocation += (rawSpeed / dist.Length) * dist;
			self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();

			return this;
		}
		
		// Todo: Duplicated from HeliAttack
		const float Epsilon = .5f;
		float2 GetRepulseForce(Actor self, Actor h)
		{
			if (self == h)
				return float2.Zero;
			var d = self.CenterLocation - h.CenterLocation;
			if (d.LengthSquared < Epsilon)
				return float2.FromAngle((float)self.World.SharedRandom.NextDouble() * 3.14f);

			return (2 / d.LengthSquared) * d;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
