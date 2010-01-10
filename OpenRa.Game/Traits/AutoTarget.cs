using System.Linq;

namespace OpenRa.Game.Traits
{
	class AutoTargetInfo : ITraitInfo
	{
		public object Create(Actor self) { return new AutoTarget(self); }
	}

	class AutoTarget : ITick, INotifyDamage
	{
		public AutoTarget(Actor self) {}

		void AttackTarget(Actor self, Actor target)
		{
			var attack = self.traits.WithInterface<AttackBase>().First();
			if (target != null)
				attack.ResolveOrder(self, new Order("Attack", self, target, int2.Zero, null));
		}

		float GetMaximumRange(Actor self)
		{
			return new[] { self.Info.Primary, self.Info.Secondary }
				.Where(w => w != null)
				.Max(w => Rules.WeaponInfo[w].Range);
		}

		public void Tick(Actor self)
		{
			if (!self.IsIdle) return;

			var attack = self.traits.WithInterface<AttackBase>().First();
			var range = GetMaximumRange(self);
			
			if (attack.target == null || 
				(attack.target.Location - self.Location).LengthSquared > range * range + 2)
				AttackTarget(self, ChooseTarget(self, range));
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = Game.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

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

			var attack = self.traits.WithInterface<AttackBase>().First();
			if (attack.target != null) return;

			AttackTarget(self, e.Attacker);
		}
	}
}
