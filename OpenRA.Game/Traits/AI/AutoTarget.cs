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

using System.Linq;

namespace OpenRA.Traits
{
	class AutoTargetInfo : StatelessTraitInfo<AutoTarget> { }

	class AutoTarget : ITick, INotifyDamage
	{
		void AttackTarget(Actor self, Actor target)
		{
			var attack = self.traits.Get<AttackBase>();
			if (target != null)
				attack.ResolveOrder(self, new Order("Attack", self, target));
		}

		public void Tick(Actor self)
		{
			if (!self.IsIdle) return;

			var attack = self.traits.Get<AttackBase>();
			var range = Util.GetMaximumRange(self);
			
			if (attack.target == null || 
				(attack.target.Location - self.Location).LengthSquared > range * range + 2)
				AttackTarget(self, ChooseTarget(self, range));
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return inRange
				.Where(a => a.Owner != null && a.Owner != self.Owner)	/* todo: one day deal with friendly players */
				.Where(a => Combat.HasAnyValidWeapons(self, a))
				.OrderBy(a => (a.Location - self.Location).LengthSquared)
				.FirstOrDefault();
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			// not a lot we can do about things we can't hurt... although maybe we should automatically run away?
			if (!Combat.HasAnyValidWeapons(self, e.Attacker))
				return;

			if (e.Attacker.Owner == self.Owner)
				return;	// don't retaliate against own units force-firing on us. it's usually not what the player wanted.

			if (e.Damage < 0)
				return;	// don't retaliate against healers

			var attack = self.traits.Get<AttackBase>();
			if (attack.target != null) return;

			AttackTarget(self, e.Attacker);
		}
	}
}
