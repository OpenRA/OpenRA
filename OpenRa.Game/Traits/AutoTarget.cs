using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class AutoTarget : ITick, INotifyDamage
	{
		public AutoTarget(Actor self) {}

		void AttackTarget(Actor self, Actor target)
		{
			var attack = self.traits.WithInterface<AttackBase>().First();
			if (target != null)
				attack.ResolveOrder(self, new Order("Attack", self, target, int2.Zero, null));
		}

		public void Tick(Actor self)
		{
			if (!self.IsIdle) return;

			var attack = self.traits.WithInterface<AttackBase>().First();
			var range = Rules.WeaponInfo[self.Info.Primary].Range;
			
			if (attack.target == null || 
				(attack.target.Location - self.Location).LengthSquared > range * range + 2)
				attack.target = ChooseTarget(self, range);
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = Game.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return inRange.Where(a => a.Owner != null && a.Owner != self.Owner)	/* todo: one day deal with friendly players */
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

			var attack = self.traits.WithInterface<AttackBase>().First();
			if (attack.target != null) return;

			AttackTarget(self, e.Attacker);
		}
	}
}
