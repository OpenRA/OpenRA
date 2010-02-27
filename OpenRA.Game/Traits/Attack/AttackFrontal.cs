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

namespace OpenRA.Traits
{
	abstract class AttackFrontal : AttackBase
	{
		public AttackFrontal(Actor self, int facingTolerance)
			: base(self) { FacingTolerance = facingTolerance; }

		readonly int FacingTolerance;

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (target == null) return;

			var unit = self.traits.Get<Unit>();
			var facingToTarget = Util.GetFacing(target.CenterLocation - self.CenterLocation, unit.Facing);

			if (Math.Abs(facingToTarget - unit.Facing) % 256 < FacingTolerance)
				DoAttack(self);
		}
	}
}
