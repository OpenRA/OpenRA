#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Used together with AttackMedic: to make the healer do it's job automatically to nearby units.")]
	class AutoHealInfo : TraitInfo<AutoHeal>, Requires<AttackBaseInfo> { }

	class AutoHeal : INotifyIdle
	{
		public void TickIdle(Actor self)
		{
			var attack = self.Trait<AttackBase>();
			var inRange = self.World.FindActorsInCircle(self.CenterPosition, attack.GetMaximumRange());

			var target = inRange
				.Where(a => a != self && a.AppearsFriendlyTo(self))
				.Where(a => a.IsInWorld && !a.IsDead)
				.Where(a => a.GetDamageState() > DamageState.Undamaged)
				.Where(a => attack.HasAnyValidWeapons(Target.FromActor(a)))
				.ClosestTo(self);

			if (target != null)
				self.QueueActivity(attack.GetAttackActivity(self, Target.FromActor(target), false));
		}
	}
}
