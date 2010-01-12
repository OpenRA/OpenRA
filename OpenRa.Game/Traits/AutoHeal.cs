using System.Linq;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class AutoHealInfo : StatelessTraitInfo<AutoHeal> { }

	class AutoHeal : ITick
	{
		void AttackTarget(Actor self, Actor target)
		{
			var attack = self.traits.Get<AttackBase>();
			if (target != null)
				attack.ResolveOrder(self, new Order("Attack", self, target, int2.Zero, null));
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
			var attack = self.traits.Get<AttackBase>();
			var range = Util.GetMaximumRange(self);

			if (NeedsNewTarget(self))
				AttackTarget(self, ChooseTarget(self, range));
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = Game.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return inRange
				.Where(a => a.Owner == self.Owner && a != self)	/* todo: one day deal with friendly players */
				.Where(a => Combat.HasAnyValidWeapons(self, a))
				.Where(a => a.Health < a.Info.Traits.Get<OwnedActorInfo>().HP)
				.OrderBy(a => (a.Location - self.Location).LengthSquared)
				.FirstOrDefault();
		}
	}
}
