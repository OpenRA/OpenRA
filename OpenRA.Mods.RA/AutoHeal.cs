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
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class AutoHealInfo : TraitInfo<AutoHeal> { }

	class AutoHeal : ITick
	{
		void AttackTarget(Actor self, Actor target)
		{
			var attack = self.traits.Get<AttackBase>();
			if (target != null)
				attack.ResolveOrder(self, new Order("Attack", self, target));
			else
				if (self.GetCurrentActivity() is Attack)
					self.CancelActivity();
		}

		bool NeedsNewTarget(Actor self)
		{
			var attack = self.traits.Get<AttackBase>();
			var range = Util.GetMaximumRange(self);

			if (attack.target == null)
				return true;	// he's dead.
			if ((attack.target.Location - self.Location).LengthSquared > range * range + 2)
				return true;	// wandered off faster than we could follow
			if (attack.target.Health == attack.target.Info.Traits.Get<OwnedActorInfo>().HP)
				return true;	// fully healed

			return false;
		}

		public void Tick(Actor self)
		{
			var range = Util.GetMaximumRange(self);

			if (NeedsNewTarget(self))
				AttackTarget(self, ChooseTarget(self, range));
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return inRange
				.Where(a => a != self && self.Owner.Stances[ a.Owner ] == Stance.Ally)
				.Where(a => Combat.HasAnyValidWeapons(self, a))
				.Where(a => a.Health < a.Info.Traits.Get<OwnedActorInfo>().HP)
				.OrderBy(a => (a.Location - self.Location).LengthSquared)
				.FirstOrDefault();
		}
	}
}
