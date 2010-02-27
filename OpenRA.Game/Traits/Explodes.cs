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

using OpenRA.Effects;

namespace OpenRA.Traits
{
	class ExplodesInfo : StatelessTraitInfo<Explodes> { }

	class Explodes : INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.IsDead)
			{
				var unit = self.traits.GetOrDefault<Unit>();
				var altitude = unit != null ? unit.Altitude : 0;

				self.World.AddFrameEndTask(
					w => w.Add(new Bullet("UnitExplode", e.Attacker.Owner, e.Attacker,
						self.CenterLocation.ToInt2(), self.CenterLocation.ToInt2(),
						altitude, altitude)));
			}
		}
	}
}
