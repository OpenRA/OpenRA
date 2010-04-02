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
using System.Drawing;

namespace OpenRA.Traits
{
	class AutoTargetInfo : StatelessTraitInfo<AutoTarget>
	{
		public readonly float ScanTimeAverage = 2f;
		public readonly float ScanTimeSpread = .5f;
	}

	class AutoTarget : ITick, INotifyDamage
	{
		int nextScanTime = 0;

		void AttackTarget(Actor self, Actor target)
		{
			var attack = self.traits.Get<AttackBase>();
			if (target != null)
				attack.ResolveOrder(self, new Order("Attack", self, target));
		}

		public void Tick(Actor self)
		{
			if (!self.IsIdle) return;

			if (--nextScanTime <= 0)
			{
				var attack = self.traits.Get<AttackBase>();
				var range = Util.GetMaximumRange(self);

				if( attack.target == null ||
					( attack.target.Location - self.Location ).LengthSquared > range * range )
					attack.target = ChooseTarget( self, range );

				var info = self.Info.Traits.Get<AutoTargetInfo>();
				nextScanTime = (int)(25 * (info.ScanTimeAverage + 
					(self.World.SharedRandom.NextDouble() * 2 - 1) * info.ScanTimeSpread));
			}
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return inRange
				.Where(a => a.Owner != null && self.Owner.Stances[ a.Owner ] == Stance.Enemy)
				.Where(a => Combat.HasAnyValidWeapons(self, a))
				.OrderBy(a => (a.Location - self.Location).LengthSquared)
				.FirstOrDefault();
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!self.IsIdle) return;

			var attack = self.traits.Get<AttackBase>();
			var range = Util.GetMaximumRange(self);

			if( attack.target != null && ( attack.target.Location - self.Location ).LengthSquared <= range * range )
				return;

			// not a lot we can do about things we can't hurt... although maybe we should automatically run away?
			if (!Combat.HasAnyValidWeapons(self, e.Attacker)) return;

			// don't retaliate against own units force-firing on us. it's usually not what the player wanted.
			if (self.Owner.Stances[e.Attacker.Owner] == Stance.Ally) return;

			if (e.Damage < 0) return;	// don't retaliate against healers

			attack.target = e.Attacker;
		}
	}
}
