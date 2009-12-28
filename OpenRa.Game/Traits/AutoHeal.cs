using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class AutoHeal : ITick
	{
		public AutoHeal(Actor self) { }

		void AttackTarget(Actor self, Actor target)
		{
			var attack = self.traits.WithInterface<AttackBase>().First();
			if (target != null)
				attack.ResolveOrder(self, new Order("Attack", self, target, int2.Zero, null));
			else
				if (!(self.GetCurrentActivity() is Move))
					self.CancelActivity();
		}

		float GetMaximumRange(Actor self)
		{
			return new[] { self.Info.Primary, self.Info.Secondary }
				.Where(w => w != null)
				.Max(w => Rules.WeaponInfo[w].Range);
		}

		bool NeedsNewTarget(Actor self)
		{
			var attack = self.traits.WithInterface<AttackBase>().First();
			var range = GetMaximumRange(self);

			if (attack.target == null)
				return true;	// he's dead.
			if ((attack.target.Location - self.Location).LengthSquared > range * range + 2)
				return true;	// wandered off faster than we could follow
			if (attack.target.Health == attack.target.Info.Strength)
				return true;	// fully healed

			return false;
		}

		public void Tick(Actor self)
		{
			var attack = self.traits.WithInterface<AttackBase>().First();
			var range = GetMaximumRange(self);

			if (NeedsNewTarget(self))
				AttackTarget(self, ChooseTarget(self, range));
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = Game.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return inRange
				.Where(a => a.Owner == self.Owner)	/* todo: one day deal with friendly players */
				.Where(a => Combat.HasAnyValidWeapons(self, a))
				.Where(a => a.Health < a.Info.Strength)
				.OrderBy(a => (a.Location - self.Location).LengthSquared)
				.FirstOrDefault();
		}
	}
}
