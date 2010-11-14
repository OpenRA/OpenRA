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
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class AutoHealInfo : TraitInfo<AutoHeal> { }

	class AutoHeal : INotifyIdle
	{
		public void Idle( Actor self )
		{
			self.QueueActivity( new IdleHealActivity() );
		}

		class IdleHealActivity : Idle
		{
			Actor currentTarget;

			public override IActivity Tick( Actor self )
			{
				if( NextActivity != null )
					return NextActivity;

				var attack = self.Trait<AttackBase>();
				var range = attack.GetMaximumRange();

				if (NeedsNewTarget(self))
					AttackTarget(self, ChooseTarget(self, range));

				return this;
			}

			void AttackTarget(Actor self, Actor target)
			{
				var attack = self.Trait<AttackBase>();
				if (target != null)
					attack.AttackTarget(Target.FromActor( target), false, true );
				else
					if (attack.IsAttacking)
						self.CancelActivity();
			}

			bool NeedsNewTarget(Actor self)
			{
				var attack = self.Trait<AttackBase>();
				var range = attack.GetMaximumRange();

				if (currentTarget == null || !currentTarget.IsInWorld)
					return true;	// he's dead.
				if( !Combat.IsInRange( self.CenterLocation, range, currentTarget ) )
					return true;	// wandered off faster than we could follow

				if (currentTarget.GetDamageState() == DamageState.Undamaged)
					return true;	// fully healed

				return false;
			}

			Actor ChooseTarget(Actor self, float range)
			{
				var inRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);
				var attack = self.Trait<AttackBase>();

				return inRange
					.Where(a => a != self && self.Owner.Stances[a.Owner] == Stance.Ally)
					.Where(a => a.IsInWorld && !a.IsDead())
					.Where(a => a.HasTrait<Health>() && a.GetDamageState() > DamageState.Undamaged)
					.Where(a => attack.HasAnyValidWeapons(Target.FromActor(a)))
					.OrderBy(a => (a.CenterLocation - self.CenterLocation).LengthSquared)
					.FirstOrDefault();
			}
		}
	}
}
