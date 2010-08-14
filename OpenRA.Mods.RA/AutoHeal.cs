#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AutoHealInfo : TraitInfo<AutoHeal> { }

	class AutoHeal : ITick
	{
		void AttackTarget(Actor self, Actor target)
		{
			var attack = self.Trait<AttackBase>();
			if (target != null)
				attack.ResolveOrder(self, new Order("Attack", self, target));
			else
				if (self.GetCurrentActivity() is Attack)
					self.CancelActivity();
		}

		bool NeedsNewTarget(Actor self)
		{
			var attack = self.Trait<AttackBase>();
			var range = attack.GetMaximumRange();

			if (!attack.target.IsValid)
				return true;	// he's dead.
			if ((attack.target.CenterLocation - self.Location).LengthSquared > range * range + 2)
				return true;	// wandered off faster than we could follow
			
			if (attack.target.IsActor
			    && attack.target.Actor.GetDamageState() == DamageState.Undamaged)
				return true;	// fully healed

			return false;
		}

		public void Tick(Actor self)
		{
			var attack = self.Trait<AttackBase>();
			var range = attack.GetMaximumRange();

			if (NeedsNewTarget(self))
				AttackTarget(self, ChooseTarget(self, range));
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);
			var attack = self.Trait<AttackBase>();

			return inRange
				.Where(a => a != self && self.Owner.Stances[a.Owner] == Stance.Ally)
				.Where(a => !a.IsDead())
				.Where(a => a.HasTrait<Health>() && a.GetDamageState() > DamageState.Undamaged)
				.Where(a => attack.HasAnyValidWeapons(Target.FromActor(a)))
				.OrderBy(a => (a.Location - self.Location).LengthSquared)
				.FirstOrDefault();
		}
	}
}
